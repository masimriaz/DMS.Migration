using System;
using System.Collections.Generic;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Domain.Entities;

public class MigrationPlan
{
    public Guid Id { get; set; }
    public int TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int? SourceConnectionId { get; set; }
    public int? TargetConnectionId { get; set; }

    public PlanStatus Status { get; set; }

    // Configuration stored as JSONB
    public string ConfigurationJson { get; set; } = "{}";

    // Mapping rules stored as JSONB
    public string MappingRulesJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Connection? SourceConnection { get; set; }
    public Connection? TargetConnection { get; set; }
    public ICollection<MigrationJob> MigrationJobs { get; set; } = new List<MigrationJob>();
}
