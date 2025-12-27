using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Text.Json;
using DMS.Migration.Application.Interfaces;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;
using DMS.Migration.Infrastructure.Data;
using DMS.Migration.Infrastructure.SharePoint;

namespace DMS.Migration.Infrastructure.Services.Scanners;

/// <summary>
/// SharePoint Online discovery scanner using Microsoft Graph API
/// </summary>
public class SharePointOnlineDiscoveryScanner : IDiscoveryScanner
{
    private readonly AppDbContext _context;
    private readonly SharePointAuthenticationService _authService;
    private readonly ILogger<SharePointOnlineDiscoveryScanner> _logger;
    private readonly IDiscoveryService _discoveryService;

    public SharePointOnlineDiscoveryScanner(
        AppDbContext context,
        SharePointAuthenticationService authService,
        IDiscoveryService discoveryService,
        ILogger<SharePointOnlineDiscoveryScanner> logger)
    {
        _context = context;
        _authService = authService;
        _discoveryService = discoveryService;
        _logger = logger;
    }

    public async Task<bool> ValidateConnectionAsync(Connection connection, CancellationToken cancellationToken = default)
    {
        try
        {
            var graphClient = await _authService.GetGraphClientAsync(connection);
            var sites = await graphClient.Sites.GetAsync(cancellationToken: cancellationToken);
            return sites != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate connection {ConnectionId}", connection.Id);
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GetConnectionInfoAsync(Connection connection, CancellationToken cancellationToken = default)
    {
        var info = new Dictionary<string, object>();

        try
        {
            var graphClient = await _authService.GetGraphClientAsync(connection);

            // Get organization info
            var org = await graphClient.Organization.GetAsync(cancellationToken: cancellationToken);
            if (org?.Value?.Any() == true)
            {
                var orgData = org.Value.First();
                info["TenantId"] = orgData.Id ?? "Unknown";
                info["DisplayName"] = orgData.DisplayName ?? "Unknown";
            }

            info["Status"] = "Connected";
        }
        catch (Exception ex)
        {
            info["Status"] = "Failed";
            info["Error"] = ex.Message;
            _logger.LogError(ex, "Failed to get connection info for {ConnectionId}", connection.Id);
        }

        return info;
    }

    public async Task ExecuteScanAsync(DiscoveryRun run, Connection connection, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting discovery scan {RunId} for connection {ConnectionId}", run.Id, connection.Id);

        try
        {
            // Update progress: Starting
            await _discoveryService.UpdateProgressAsync(run.Id, 5, "Initializing scan", cancellationToken);

            // Validate connection
            var isValid = await ValidateConnectionAsync(connection, cancellationToken);
            if (!isValid)
            {
                throw new InvalidOperationException("Connection validation failed");
            }

            await _discoveryService.UpdateProgressAsync(run.Id, 10, "Connection validated", cancellationToken);

            // Get Graph client
            var graphClient = await _authService.GetGraphClientAsync(connection);

            // Scan sites
            await ScanSitesAsync(run, graphClient, cancellationToken);

            // Calculate metrics
            await CalculateMetricsAsync(run, cancellationToken);

            // Complete the run
            await _discoveryService.CompleteDiscoveryRunAsync(run.Id, cancellationToken);

            _logger.LogInformation("Discovery scan {RunId} completed successfully", run.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Discovery scan {RunId} failed: {Error}", run.Id, ex.Message);
            await _discoveryService.FailDiscoveryRunAsync(run.Id, ex.Message, cancellationToken);
            throw;
        }
    }

    private async Task ScanSitesAsync(DiscoveryRun run, GraphServiceClient graphClient, CancellationToken cancellationToken)
    {
        await _discoveryService.UpdateProgressAsync(run.Id, 20, "Scanning sites", cancellationToken);

        try
        {
            // Get all sites (this is a simplified approach - in production you'd paginate)
            var sitesResponse = await graphClient.Sites
                .GetAsync(config =>
                {
                    config.QueryParameters.Top = 100;
                }, cancellationToken);

            if (sitesResponse?.Value == null) return;

            var totalSites = sitesResponse.Value.Count;
            var processedSites = 0;

            foreach (var site in sitesResponse.Value)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await ScanSiteAsync(run, graphClient, site, null, 0, cancellationToken);

                processedSites++;
                var progress = 20 + (int)((processedSites / (double)totalSites) * 50);
                await _discoveryService.UpdateProgressAsync(run.Id, progress, $"Scanned {processedSites}/{totalSites} sites", cancellationToken);
            }

            // Update run statistics
            run.TotalSitesScanned = processedSites;
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning sites for run {RunId}", run.Id);

            // Add warning
            _context.DiscoveryWarnings.Add(new DiscoveryWarning
            {
                Id = Guid.NewGuid(),
                DiscoveryRunId = run.Id,
                TenantId = run.TenantId,
                Severity = DiscoverySeverity.Error,
                Category = "SiteScanning",
                Code = "SITE_SCAN_ERROR",
                Message = "Failed to scan some sites",
                DetailedMessage = ex.Message,
                DetectedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ScanSiteAsync(
        DiscoveryRun run,
        GraphServiceClient graphClient,
        Microsoft.Graph.Models.Site site,
        Guid? parentItemId,
        int level,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create discovery item for site
            var siteItem = new DiscoveryItem
            {
                Id = Guid.NewGuid(),
                DiscoveryRunId = run.Id,
                TenantId = run.TenantId,
                ItemType = level == 0 ? DiscoveryItemType.SiteCollection : DiscoveryItemType.Site,
                ParentItemId = parentItemId,
                Level = level,
                Path = site.WebUrl ?? "",
                Title = site.DisplayName ?? site.Name ?? "Unknown",
                SharePointId = Guid.Parse(site.Id?.Split(',').Last() ?? Guid.NewGuid().ToString()),
                DiscoveredAt = DateTime.UtcNow,
                CreatedDate = site.CreatedDateTime?.UtcDateTime,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    site.Id,
                    site.Name,
                    site.Description
                })
            };

            _context.DiscoveryItems.Add(siteItem);
            await _context.SaveChangesAsync(cancellationToken);

            // Scan lists/libraries for this site
            await ScanListsAsync(run, graphClient, site, siteItem.Id, cancellationToken);

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan site {SiteId} in run {RunId}", site.Id, run.Id);

            _context.DiscoveryWarnings.Add(new DiscoveryWarning
            {
                Id = Guid.NewGuid(),
                DiscoveryRunId = run.Id,
                TenantId = run.TenantId,
                Severity = DiscoverySeverity.Warning,
                Category = "SiteScanning",
                Code = "SITE_ACCESS_ERROR",
                Message = $"Could not fully scan site: {site.DisplayName}",
                ItemPath = site.WebUrl,
                DetailedMessage = ex.Message,
                DetectedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ScanListsAsync(
        DiscoveryRun run,
        GraphServiceClient graphClient,
        Microsoft.Graph.Models.Site site,
        Guid siteItemId,
        CancellationToken cancellationToken)
    {
        try
        {
            var lists = await graphClient.Sites[site.Id].Lists
                .GetAsync(cancellationToken: cancellationToken);

            if (lists?.Value == null) return;

            foreach (var list in lists.Value)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await ScanListAsync(run, graphClient, site, list, siteItemId, cancellationToken);
            }

            run.TotalListsScanned += lists.Value.Count;
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan lists for site {SiteId}", site.Id);
        }
    }

    private async Task ScanListAsync(
        DiscoveryRun run,
        GraphServiceClient graphClient,
        Microsoft.Graph.Models.Site site,
        Microsoft.Graph.Models.List list,
        Guid siteItemId,
        CancellationToken cancellationToken)
    {
        try
        {
            var isLibrary = (list.ListProp?.Template?.Contains("documentLibrary", StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (list.Name?.EndsWith("Documents", StringComparison.OrdinalIgnoreCase) ?? false);

            // Get item count (simplified - would need pagination for accuracy)
            var itemsResponse = await graphClient.Sites[site.Id].Lists[list.Id].Items
                .GetAsync(config => config.QueryParameters.Top = 1, cancellationToken);

            var itemCount = itemsResponse?.Value?.Count ?? 0;

            // Check for versioning - default to false if property not available
            var versioningEnabled = false; // Graph SDK might not expose this directly

            // Create discovery item
            var listItem = new DiscoveryItem
            {
                Id = Guid.NewGuid(),
                DiscoveryRunId = run.Id,
                TenantId = run.TenantId,
                ItemType = isLibrary ? DiscoveryItemType.Library : DiscoveryItemType.List,
                ParentItemId = siteItemId,
                Level = 1,
                Path = $"{site.WebUrl}/Lists/{list.Name}",
                Title = list.DisplayName ?? list.Name ?? "Unknown",
                InternalName = list.Name,
                SharePointId = Guid.Parse(list.Id ?? Guid.NewGuid().ToString()),
                ItemCount = itemCount,
                VersioningEnabled = versioningEnabled,
                TemplateType = list.ListProp?.Template,
                DiscoveredAt = DateTime.UtcNow,
                CreatedDate = list.CreatedDateTime?.UtcDateTime,
                ModifiedDate = list.LastModifiedDateTime?.UtcDateTime,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    list.Id,
                    list.Name,
                    list.Description,
                    Template = list.ListProp?.Template
                })
            };

            _context.DiscoveryItems.Add(listItem);
            run.TotalItemsScanned += itemCount;
            await _context.SaveChangesAsync(cancellationToken);

            // Check for issues
            await CheckListIssuesAsync(run, listItem, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan list {ListId} in site {SiteId}", list.Id, site.Id);
        }
    }

    private async Task CheckListIssuesAsync(DiscoveryRun run, DiscoveryItem listItem, CancellationToken cancellationToken)
    {
        // Check for high version counts (warning)
        if (listItem.VersioningEnabled == true && listItem.MajorVersionLimit > 500)
        {
            _context.DiscoveryWarnings.Add(new DiscoveryWarning
            {
                Id = Guid.NewGuid(),
                DiscoveryRunId = run.Id,
                TenantId = run.TenantId,
                DiscoveryItemId = listItem.Id,
                Severity = DiscoverySeverity.Warning,
                Category = "Versioning",
                Code = "HIGH_VERSION_LIMIT",
                Message = "Library has high version limit which may increase migration time",
                ItemPath = listItem.Path,
                DetailedMessage = $"Version limit: {listItem.MajorVersionLimit}. Consider reviewing version settings before migration.",
                DetectedAt = DateTime.UtcNow
            });
        }

        // Check for large libraries
        if (listItem.ItemCount > 5000)
        {
            _context.DiscoveryWarnings.Add(new DiscoveryWarning
            {
                Id = Guid.NewGuid(),
                DiscoveryRunId = run.Id,
                TenantId = run.TenantId,
                DiscoveryItemId = listItem.Id,
                Severity = DiscoverySeverity.Info,
                Category = "Performance",
                Code = "LARGE_LIBRARY",
                Message = "Large library detected",
                ItemPath = listItem.Path,
                DetailedMessage = $"Library contains {listItem.ItemCount} items. Consider batching migration.",
                RecommendationJson = JsonSerializer.Serialize(new
                {
                    Recommendation = "Enable incremental migration",
                    BatchSize = 1000
                }),
                DetectedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task CalculateMetricsAsync(DiscoveryRun run, CancellationToken cancellationToken)
    {
        await _discoveryService.UpdateProgressAsync(run.Id, 90, "Calculating metrics", cancellationToken);

        var items = await _context.DiscoveryItems
            .Where(i => i.DiscoveryRunId == run.Id)
            .ToListAsync(cancellationToken);

        var metrics = new List<DiscoveryMetric>
        {
            new()
            {
                Id = Guid.NewGuid(),
                DiscoveryRunId = run.Id,
                TenantId = run.TenantId,
                MetricKey = "total_sites",
                MetricCategory = "Summary",
                NumericValue = items.Count(i => i.ItemType == DiscoveryItemType.Site || i.ItemType == DiscoveryItemType.SiteCollection),
                CalculatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                DiscoveryRunId = run.Id,
                TenantId = run.TenantId,
                MetricKey = "total_libraries",
                MetricCategory = "Summary",
                NumericValue = items.Count(i => i.ItemType == DiscoveryItemType.Library),
                CalculatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                DiscoveryRunId = run.Id,
                TenantId = run.TenantId,
                MetricKey = "total_lists",
                MetricCategory = "Summary",
                NumericValue = items.Count(i => i.ItemType == DiscoveryItemType.List),
                CalculatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                DiscoveryRunId = run.Id,
                TenantId = run.TenantId,
                MetricKey = "total_items",
                MetricCategory = "Summary",
                NumericValue = items.Sum(i => i.ItemCount),
                CalculatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                DiscoveryRunId = run.Id,
                TenantId = run.TenantId,
                MetricKey = "total_size_bytes",
                MetricCategory = "Summary",
                NumericValue = items.Sum(i => i.SizeInBytes),
                CalculatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                DiscoveryRunId = run.Id,
                TenantId = run.TenantId,
                MetricKey = "libraries_with_versioning",
                MetricCategory = "Features",
                NumericValue = items.Count(i => i.ItemType == DiscoveryItemType.Library && i.VersioningEnabled == true),
                CalculatedAt = DateTime.UtcNow
            }
        };

        _context.DiscoveryMetrics.AddRange(metrics);
        await _context.SaveChangesAsync(cancellationToken);

        await _discoveryService.UpdateProgressAsync(run.Id, 95, "Metrics calculated", cancellationToken);
    }
}
