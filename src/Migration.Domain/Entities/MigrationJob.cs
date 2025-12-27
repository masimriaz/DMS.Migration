using System;
using System.Collections.Generic;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Domain.Entities;

public class MigrationJob
{
    public Guid Id { get; set; }
    public int TenantId { get; set; }
    public Guid? MigrationPlanId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public JobType Type { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Draft;

    public int? SourceConnectionId { get; set; }
    public int? TargetConnectionId { get; set; }

    // Configuration serialized as JSONB for flexibility
    public string ConfigurationJson { get; set; } = "{}";

    // Progress tracking
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SuccessfulItems { get; set; }
    public int FailedItems { get; set; }
    public int SkippedItems { get; set; }

    public long TotalBytes { get; set; }
    public long ProcessedBytes { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string? ErrorMessage { get; set; }
    public string? DiagnosticsJson { get; set; } // JSONB - detailed error info

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public MigrationPlan? MigrationPlan { get; set; }
    public Connection? SourceConnection { get; set; }
    public Connection? TargetConnection { get; set; }
    public ICollection<MigrationTask> Tasks { get; set; } = new List<MigrationTask>();
    public ICollection<ValidationRun> ValidationRuns { get; set; } = new List<ValidationRun>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<JobLog> JobLogs { get; set; } = new List<JobLog>();
}

public class MigrationTask
{
    public Guid Id { get; set; }
    public Guid MigrationJobId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.Pending;

    public int Sequence { get; set; }

    public string? SourcePath { get; set; }
    public string? TargetPath { get; set; }

    public string TaskDataJson { get; set; } = "{}"; // JSONB - task-specific data

    public int ItemsProcessed { get; set; }
    public int ItemsTotal { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;

    // Navigation
    public MigrationJob MigrationJob { get; set; } = null!;
    public ICollection<TaskCheckpoint> Checkpoints { get; set; } = new List<TaskCheckpoint>();
    public ICollection<JobLog> JobLogs { get; set; } = new List<JobLog>();
}