using System;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Domain.Entities;

public class JobLog
{
    public Guid Id { get; set; }
    public Guid MigrationJobId { get; set; }
    public Guid? MigrationTaskId { get; set; }

    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;

    public string? Exception { get; set; }
    public string? StackTrace { get; set; }

    public string DetailsJson { get; set; } = "{}"; // JSONB - structured log data

    public DateTime Timestamp { get; set; }

    // Navigation
    public MigrationJob MigrationJob { get; set; } = null!;
    public MigrationTask? MigrationTask { get; set; }
}
