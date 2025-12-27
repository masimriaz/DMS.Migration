using System;
using System.Collections.Generic;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Domain.Entities;

public class ValidationRun
{
    public Guid Id { get; set; }
    public int TenantId { get; set; }
    public Guid? MigrationJobId { get; set; }

    public string Name { get; set; } = string.Empty;
    public ValidationType Type { get; set; }
    public ValidationStatus Status { get; set; }

    public int? SourceConnectionId { get; set; }
    public int? TargetConnectionId { get; set; }

    public string ParametersJson { get; set; } = "{}"; // JSONB

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int TotalChecks { get; set; }
    public int PassedChecks { get; set; }
    public int FailedChecks { get; set; }
    public int WarningChecks { get; set; }

    public string? ErrorMessage { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public MigrationJob? MigrationJob { get; set; }
    public Connection? SourceConnection { get; set; }
    public Connection? TargetConnection { get; set; }
    public ICollection<ValidationFinding> Findings { get; set; } = new List<ValidationFinding>();
}
