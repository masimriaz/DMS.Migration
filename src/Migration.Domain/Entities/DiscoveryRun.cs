using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Domain.Entities;

/// <summary>
/// Represents a discovery run that scans a SharePoint source to inventory sites, lists, libraries, and metadata
/// </summary>
public class DiscoveryRun
{
    public Guid Id { get; set; }
    public int TenantId { get; set; }
    public int SourceConnectionId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Correlation ID for distributed tracing
    public string CorrelationId { get; set; } = string.Empty;

    public DiscoveryStatus Status { get; set; } = DiscoveryStatus.Queued;

    // Scope and configuration
    public string ScopeUrl { get; set; } = string.Empty; // Root URL or specific site/library
    public string ConfigurationJson { get; set; } = "{}"; // JSONB - includes toggles for versions, permissions, etc.

    // Progress tracking
    public int ProgressPercentage { get; set; }
    public string? CurrentStep { get; set; } // e.g., "Scanning Sites", "Analyzing Libraries"
    public int TotalSitesScanned { get; set; }
    public int TotalListsScanned { get; set; }
    public int TotalItemsScanned { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Error handling
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    // Retention
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public Connection SourceConnection { get; set; } = null!;
    public ICollection<DiscoveryItem> Items { get; set; } = new List<DiscoveryItem>();
    public ICollection<DiscoveryMetric> Metrics { get; set; } = new List<DiscoveryMetric>();
    public ICollection<DiscoveryWarning> Warnings { get; set; } = new List<DiscoveryWarning>();
    public ICollection<DiscoveryExport> Exports { get; set; } = new List<DiscoveryExport>();
}
