namespace DMS.Migration.Domain.Enums;

public enum PlanStatus
{
    Draft = 0,
    Ready = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}

public enum ValidationType
{
    PreMigration = 1,
    PostMigration = 2,
    ContentComparison = 3,
    PermissionAudit = 4,
    MetadataValidation = 5,
    BrokenLinks = 6
}

public enum ValidationStatus
{
    NotStarted = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    PartiallyCompleted = 4
}

public enum FindingSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

public enum ReportType
{
    MigrationSummary = 1,
    ValidationReport = 2,
    AuditTrail = 3,
    PermissionMatrix = 4,
    ErrorAnalysis = 5,
    ComplianceReport = 6
}

public enum ReportStatus
{
    Generating = 0,
    Ready = 1,
    Failed = 2,
    Expired = 3
}

public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}

public enum JobStatus
{
    Draft = 0,
    Pending = 1,
    Queued = 2,
    Running = 3,
    Paused = 4,
    Completed = 5,
    Failed = 6,
    Cancelled = 7
}

public enum JobType
{
    Discovery = 1,
    Migration = 2,
    Validation = 3,
    Incremental = 4
}

public enum TaskStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Skipped = 4,
    Retrying = 5
}
