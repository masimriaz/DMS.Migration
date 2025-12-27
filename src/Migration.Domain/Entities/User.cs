using System;
using System.Collections.Generic;

namespace DMS.Migration.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public int TenantId { get; set; }

    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
    public string? PasswordSalt { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; } = false;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LockedUntil { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();
}
