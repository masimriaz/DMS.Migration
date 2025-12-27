using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using DMS.Migration.Application.Interfaces;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;
using DMS.Migration.Infrastructure.Data;

namespace DMS.Migration.Infrastructure.Services;

public class DiscoveryService : IDiscoveryService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DiscoveryService> _logger;

    public DiscoveryService(AppDbContext context, ILogger<DiscoveryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DiscoveryRun> CreateDiscoveryRunAsync(
        int tenantId,
        int sourceConnectionId,
        string name,
        string scopeUrl,
        Dictionary<string, object>? configuration,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");

        var run = new DiscoveryRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SourceConnectionId = sourceConnectionId,
            Name = name,
            ScopeUrl = scopeUrl,
            CorrelationId = correlationId,
            ConfigurationJson = configuration != null ? JsonSerializer.Serialize(configuration) : "{}",
            Status = DiscoveryStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            ProgressPercentage = 0
        };

        _context.DiscoveryRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created discovery run {RunId} for tenant {TenantId}, correlation {CorrelationId}",
            run.Id, tenantId, correlationId);

        return run;
    }

    public async Task<DiscoveryRun?> GetDiscoveryRunAsync(Guid runId, int tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.DiscoveryRuns
            .Include(r => r.SourceConnection)
            .Where(r => r.Id == runId && r.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<DiscoveryRun>> GetDiscoveryRunsAsync(
        int tenantId,
        int? sourceConnectionId = null,
        DiscoveryStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DiscoveryRuns
            .Include(r => r.SourceConnection)
            .Where(r => r.TenantId == tenantId && !r.IsArchived);

        if (sourceConnectionId.HasValue)
            query = query.Where(r => r.SourceConnectionId == sourceConnectionId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateProgressAsync(Guid runId, int percentage, string currentStep, CancellationToken cancellationToken = default)
    {
        var run = await _context.DiscoveryRuns.FindAsync(new object[] { runId }, cancellationToken);
        if (run == null) return false;

        run.ProgressPercentage = Math.Clamp(percentage, 0, 100);
        run.CurrentStep = currentStep;
        run.UpdatedAt = DateTime.UtcNow;

        if (run.Status == DiscoveryStatus.Queued)
        {
            run.Status = DiscoveryStatus.Running;
            run.StartedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CompleteDiscoveryRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _context.DiscoveryRuns.FindAsync(new object[] { runId }, cancellationToken);
        if (run == null) return false;

        run.Status = DiscoveryStatus.Completed;
        run.CompletedAt = DateTime.UtcNow;
        run.ProgressPercentage = 100;
        run.CurrentStep = "Completed";
        run.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Discovery run {RunId} completed successfully", runId);
        return true;
    }

    public async Task<bool> FailDiscoveryRunAsync(Guid runId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var run = await _context.DiscoveryRuns.FindAsync(new object[] { runId }, cancellationToken);
        if (run == null) return false;

        run.Status = DiscoveryStatus.Failed;
        run.ErrorMessage = errorMessage;
        run.CompletedAt = DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogError("Discovery run {RunId} failed: {Error}", runId, errorMessage);
        return true;
    }

    public async Task<bool> CancelDiscoveryRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _context.DiscoveryRuns.FindAsync(new object[] { runId }, cancellationToken);
        if (run == null) return false;

        run.Status = DiscoveryStatus.Cancelled;
        run.CompletedAt = DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Discovery run {RunId} cancelled", runId);
        return true;
    }

    public async Task<List<DiscoveryItem>> GetDiscoveryItemsAsync(
        Guid runId,
        int tenantId,
        DiscoveryItemType? itemType = null,
        Guid? parentItemId = null,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DiscoveryItems
            .Where(i => i.DiscoveryRunId == runId && i.TenantId == tenantId);

        if (itemType.HasValue)
            query = query.Where(i => i.ItemType == itemType.Value);

        if (parentItemId.HasValue)
            query = query.Where(i => i.ParentItemId == parentItemId.Value);

        return await query
            .OrderBy(i => i.Level)
            .ThenBy(i => i.Path)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<DiscoveryItem?> GetDiscoveryItemByIdAsync(Guid itemId, int tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.DiscoveryItems
            .Include(i => i.ParentItem)
            .Where(i => i.Id == itemId && i.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetItemCountAsync(Guid runId, DiscoveryItemType? itemType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.DiscoveryItems.Where(i => i.DiscoveryRunId == runId);

        if (itemType.HasValue)
            query = query.Where(i => i.ItemType == itemType.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<List<DiscoveryMetric>> GetMetricsAsync(Guid runId, int tenantId, string? category = null, CancellationToken cancellationToken = default)
    {
        var query = _context.DiscoveryMetrics
            .Where(m => m.DiscoveryRunId == runId && m.TenantId == tenantId);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(m => m.MetricCategory == category);

        return await query
            .OrderBy(m => m.MetricCategory)
            .ThenBy(m => m.MetricKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, long>> GetSummaryMetricsAsync(Guid runId, int tenantId, CancellationToken cancellationToken = default)
    {
        var metrics = await _context.DiscoveryMetrics
            .Where(m => m.DiscoveryRunId == runId && m.TenantId == tenantId && m.MetricCategory == "Summary")
            .ToListAsync(cancellationToken);

        return metrics.ToDictionary(m => m.MetricKey, m => m.NumericValue);
    }

    public async Task<List<DiscoveryWarning>> GetWarningsAsync(
        Guid runId,
        int tenantId,
        DiscoverySeverity? minSeverity = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DiscoveryWarnings
            .Include(w => w.DiscoveryItem)
            .Where(w => w.DiscoveryRunId == runId && w.TenantId == tenantId);

        if (minSeverity.HasValue)
            query = query.Where(w => w.Severity >= minSeverity.Value);

        return await query
            .OrderByDescending(w => w.Severity)
            .ThenByDescending(w => w.DetectedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetWarningCountAsync(Guid runId, DiscoverySeverity? minSeverity = null, CancellationToken cancellationToken = default)
    {
        var query = _context.DiscoveryWarnings.Where(w => w.DiscoveryRunId == runId);

        if (minSeverity.HasValue)
            query = query.Where(w => w.Severity >= minSeverity.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<DiscoveryExport> CreateExportAsync(
        Guid runId,
        int tenantId,
        DiscoveryExportFormat format,
        DiscoveryExportType exportType,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        var run = await GetDiscoveryRunAsync(runId, tenantId, cancellationToken);
        if (run == null)
            throw new InvalidOperationException($"Discovery run {runId} not found");

        var fileName = $"discovery_{run.Name}_{exportType}_{DateTime.UtcNow:yyyyMMddHHmmss}.{format.ToString().ToLower()}";

        var export = new DiscoveryExport
        {
            Id = Guid.NewGuid(),
            DiscoveryRunId = runId,
            TenantId = tenantId,
            Format = format,
            ExportType = exportType,
            FileName = fileName,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            ExpiresAt = DateTime.UtcNow.AddDays(7) // Expires in 7 days
        };

        _context.DiscoveryExports.Add(export);
        await _context.SaveChangesAsync(cancellationToken);

        return export;
    }

    public async Task<byte[]> GenerateExportDataAsync(DiscoveryExport export, CancellationToken cancellationToken = default)
    {
        switch (export.Format)
        {
            case DiscoveryExportFormat.Json:
                return await GenerateJsonExportAsync(export, cancellationToken);

            case DiscoveryExportFormat.Csv:
                return await GenerateCsvExportAsync(export, cancellationToken);

            default:
                throw new NotSupportedException($"Export format {export.Format} not supported yet");
        }
    }

    private async Task<byte[]> GenerateJsonExportAsync(DiscoveryExport export, CancellationToken cancellationToken)
    {
        object data;

        switch (export.ExportType)
        {
            case DiscoveryExportType.Summary:
                var run = await _context.DiscoveryRuns
                    .Include(r => r.SourceConnection)
                    .FirstAsync(r => r.Id == export.DiscoveryRunId, cancellationToken);

                var metrics = await GetSummaryMetricsAsync(export.DiscoveryRunId, export.TenantId, cancellationToken);
                var warningCount = await GetWarningCountAsync(export.DiscoveryRunId, cancellationToken: cancellationToken);

                data = new
                {
                    run.Id,
                    run.Name,
                    run.Status,
                    run.CreatedAt,
                    run.CompletedAt,
                    SourceConnection = run.SourceConnection.Name,
                    Metrics = metrics,
                    WarningCount = warningCount
                };
                break;

            case DiscoveryExportType.DetailedInventory:
                var items = await _context.DiscoveryItems
                    .Where(i => i.DiscoveryRunId == export.DiscoveryRunId && i.TenantId == export.TenantId)
                    .OrderBy(i => i.Path)
                    .ToListAsync(cancellationToken);

                data = items;
                break;

            case DiscoveryExportType.WarningsOnly:
                var warnings = await GetWarningsAsync(export.DiscoveryRunId, export.TenantId,
                    pageNumber: 1, pageSize: 10000, cancellationToken: cancellationToken);

                data = warnings;
                break;

            default:
                throw new NotSupportedException($"Export type {export.ExportType} not implemented");
        }

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        return Encoding.UTF8.GetBytes(json);
    }

    private async Task<byte[]> GenerateCsvExportAsync(DiscoveryExport export, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        switch (export.ExportType)
        {
            case DiscoveryExportType.DetailedInventory:
                sb.AppendLine("Type,Path,Title,ItemCount,Size(MB),VersioningEnabled,CheckedOutItems,HasUniquePermissions");

                var items = await _context.DiscoveryItems
                    .Where(i => i.DiscoveryRunId == export.DiscoveryRunId && i.TenantId == export.TenantId)
                    .OrderBy(i => i.Path)
                    .ToListAsync(cancellationToken);

                foreach (var item in items)
                {
                    var sizeMB = item.SizeInBytes / (1024.0 * 1024.0);
                    sb.AppendLine($"{item.ItemType},{EscapeCsv(item.Path)},{EscapeCsv(item.Title)}," +
                                $"{item.ItemCount},{sizeMB:F2},{item.VersioningEnabled}," +
                                $"{item.CheckedOutItemsCount},{item.HasUniquePermissions}");
                }
                break;

            case DiscoveryExportType.WarningsOnly:
                sb.AppendLine("Severity,Category,Code,Message,ItemPath,DetectedAt");

                var warnings = await GetWarningsAsync(export.DiscoveryRunId, export.TenantId,
                    pageNumber: 1, pageSize: 10000, cancellationToken: cancellationToken);

                foreach (var warning in warnings)
                {
                    sb.AppendLine($"{warning.Severity},{EscapeCsv(warning.Category)},{EscapeCsv(warning.Code)}," +
                                $"{EscapeCsv(warning.Message)},{EscapeCsv(warning.ItemPath ?? "")}," +
                                $"{warning.DetectedAt:yyyy-MM-dd HH:mm:ss}");
                }
                break;

            default:
                throw new NotSupportedException($"CSV export for {export.ExportType} not implemented");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    public async Task<List<DiscoveryExport>> GetExportsAsync(Guid runId, int tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.DiscoveryExports
            .Where(e => e.DiscoveryRunId == runId && e.TenantId == tenantId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ArchiveOldRunsAsync(int tenantId, int keepLastN, CancellationToken cancellationToken = default)
    {
        var runsToArchive = await _context.DiscoveryRuns
            .Where(r => r.TenantId == tenantId && !r.IsArchived)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(keepLastN)
            .ToListAsync(cancellationToken);

        foreach (var run in runsToArchive)
        {
            run.IsArchived = true;
            run.ArchivedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Archived {Count} old discovery runs for tenant {TenantId}", runsToArchive.Count, tenantId);
        return runsToArchive.Count;
    }

    public async Task<int> DeleteExpiredExportsAsync(CancellationToken cancellationToken = default)
    {
        var expiredExports = await _context.DiscoveryExports
            .Where(e => e.ExpiresAt.HasValue && e.ExpiresAt.Value < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        _context.DiscoveryExports.RemoveRange(expiredExports);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted {Count} expired discovery exports", expiredExports.Count);
        return expiredExports.Count;
    }
}
