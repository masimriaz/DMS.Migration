using System;

namespace DMS.Migration.Domain.Entities;

public class TaskCheckpoint
{
    public Guid Id { get; set; }
    public Guid MigrationTaskId { get; set; }

    public string CheckpointType { get; set; } = string.Empty; // ItemProcessed, BatchCompleted, etc.
    public string CheckpointData { get; set; } = "{}"; // JSONB - stores resume data

    public int ItemsProcessed { get; set; }
    public int ItemsTotal { get; set; }
    public long BytesProcessed { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public MigrationTask MigrationTask { get; set; } = null!;
}
