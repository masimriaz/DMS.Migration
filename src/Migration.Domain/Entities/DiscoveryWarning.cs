using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Domain.Entities;

/// <summary>
/// Captures warnings and issues found during discovery
/// </summary>
public class DiscoveryWarning
{
    public Guid Id { get; set; }
    public Guid DiscoveryRunId { get; set; }
    public int TenantId { get; set; }

    public Guid? DiscoveryItemId { get; set; } // Reference to specific item if applicable

    public DiscoverySeverity Severity { get; set; }
    public string Category { get; set; } = string.Empty; // e.g., "Versioning", "Permissions", "Customization"
    public string Code { get; set; } = string.Empty; // e.g., "CHECKED_OUT_FILES", "BROKEN_LINK"

    public string Message { get; set; } = string.Empty;
    public string? DetailedMessage { get; set; }
    public string? ItemPath { get; set; } // Quick reference to affected item

    public string? RecommendationJson { get; set; } // JSONB - suggested remediation steps

    public DateTime DetectedAt { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public DiscoveryRun DiscoveryRun { get; set; } = null!;
    public DiscoveryItem? DiscoveryItem { get; set; }
}
