using System;
using System.Collections.Generic;

namespace DMS.Migration.Domain.Entities;

public class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public bool IsSystemRole { get; set; } = false;
    public int Priority { get; set; } = 0;

    public string PermissionsJson { get; set; } = "[]"; // JSONB array of permission codes

    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
