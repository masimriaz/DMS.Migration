using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Domain.Entities;

/// <summary>
/// Stores aggregate metrics for a discovery run
/// </summary>
public class DiscoveryMetric
{
    public Guid Id { get; set; }
    public Guid DiscoveryRunId { get; set; }
    public int TenantId { get; set; }

    public string MetricKey { get; set; } = string.Empty; // e.g., "total_sites", "total_documents"
    public string MetricCategory { get; set; } = string.Empty; // e.g., "Summary", "Library", "Security"

    public long NumericValue { get; set; }
    public string? StringValue { get; set; }
    public string? JsonValue { get; set; } // JSONB for complex metrics

    public DateTime CalculatedAt { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public DiscoveryRun DiscoveryRun { get; set; } = null!;
}
