using System;
using System.Collections.Generic;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Domain.Entities;

public class Report
{
    public Guid Id { get; set; }
    public int TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public ReportType Type { get; set; }
    public ReportStatus Status { get; set; }

    public Guid? MigrationJobId { get; set; }
    public Guid? ValidationRunId { get; set; }

    public string ParametersJson { get; set; } = "{}"; // JSONB

    public DateTime GeneratedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public string GeneratedBy { get; set; } = string.Empty;

    public long TotalSizeBytes { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public MigrationJob? MigrationJob { get; set; }
    public ValidationRun? ValidationRun { get; set; }
    public ICollection<ReportFile> ReportFiles { get; set; } = new List<ReportFile>();
}
