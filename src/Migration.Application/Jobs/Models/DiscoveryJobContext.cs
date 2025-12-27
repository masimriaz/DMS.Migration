namespace DMS.Migration.Application.Jobs.Models;

/// <summary>
/// Context for discovery job execution
/// </summary>
public class DiscoveryJobContext
{
    public int RunId { get; set; }
    public int TenantId { get; set; }
    public int ItemsProcessed { get; set; }
    public long TotalSizeBytes { get; set; }
    public List<string> Warnings { get; set; } = new();
    public bool IsCancelled { get; set; }
    public DateTime StartedAt { get; set; }
}
