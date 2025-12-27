using System;
using System.Collections.Generic;

namespace DMS.Migration.Domain.Entities;

public class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Connection> Connections { get; set; } = new List<Connection>();
    public ICollection<MigrationPlan> MigrationPlans { get; set; } = new List<MigrationPlan>();
    public ICollection<MigrationJob> MigrationJobs { get; set; } = new List<MigrationJob>();
    public ICollection<ValidationRun> ValidationRuns { get; set; } = new List<ValidationRun>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();
}
