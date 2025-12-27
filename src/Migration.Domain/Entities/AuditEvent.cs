using System;

namespace DMS.Migration.Domain.Entities;

public class AuditEvent
{
    public Guid Id { get; set; }
    public int TenantId { get; set; }

    public string EntityType { get; set; } = string.Empty; // Connection, MigrationJob, User, etc.
    public string EntityId { get; set; } = string.Empty; // Can be Guid or int, stored as string
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Verify, Login, etc.

    public string? OldValuesJson { get; set; } // JSONB - state before change
    public string? NewValuesJson { get; set; } // JSONB - state after change
    public string? MetadataJson { get; set; } // JSONB - additional context

    public DateTime Timestamp { get; set; }

    public Guid? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public User? User { get; set; }
}
