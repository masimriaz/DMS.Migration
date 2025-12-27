using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Domain.Entities;

/// <summary>
/// Tracks exported reports from discovery runs
/// </summary>
public class DiscoveryExport
{
    public Guid Id { get; set; }
    public Guid DiscoveryRunId { get; set; }
    public int TenantId { get; set; }

    public DiscoveryExportFormat Format { get; set; }
    public DiscoveryExportType ExportType { get; set; } // Summary, Detailed, Warnings, etc.

    public string FileName { get; set; } = string.Empty;
    public string? FilePath { get; set; } // Optional: if storing on disk
    public long FileSizeBytes { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; } // For cleanup
    public bool IsDownloaded { get; set; }
    public int DownloadCount { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public DiscoveryRun DiscoveryRun { get; set; } = null!;
}
