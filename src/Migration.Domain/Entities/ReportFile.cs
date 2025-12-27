using System;

namespace DMS.Migration.Domain.Entities;

public class ReportFile
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // PDF, Excel, CSV, HTML
    public long SizeBytes { get; set; }

    public string StoragePath { get; set; } = string.Empty;
    public string? ContentHash { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Report Report { get; set; } = null!;
}
