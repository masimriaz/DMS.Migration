using Microsoft.EntityFrameworkCore;
using DMS.Migration.Domain.Entities;
using System.Text.RegularExpressions;

namespace DMS.Migration.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Core / Multi-Tenancy Schema
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    // Connections Schema
    public DbSet<Connection> Connections { get; set; }
    public DbSet<ConnectionSecret> ConnectionSecrets { get; set; }
    public DbSet<ConnectionVerificationRun> ConnectionVerificationRuns { get; set; }

    // Migration Schema
    public DbSet<MigrationPlan> MigrationPlans { get; set; }
    public DbSet<MigrationJob> MigrationJobs { get; set; }
    public DbSet<MigrationTask> MigrationTasks { get; set; }
    public DbSet<TaskCheckpoint> TaskCheckpoints { get; set; }

    // Validation & Reporting Schema
    public DbSet<ValidationRun> ValidationRuns { get; set; }
    public DbSet<ValidationFinding> ValidationFindings { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<ReportFile> ReportFiles { get; set; }

    // Auditing & Observability Schema
    public DbSet<AuditEvent> AuditEvents { get; set; }
    public DbSet<JobLog> JobLogs { get; set; }

    // Discovery Schema
    public DbSet<DiscoveryRun> DiscoveryRuns { get; set; }
    public DbSet<DiscoveryItem> DiscoveryItems { get; set; }
    public DbSet<DiscoveryMetric> DiscoveryMetrics { get; set; }
    public DbSet<DiscoveryWarning> DiscoveryWarnings { get; set; }
    public DbSet<DiscoveryExport> DiscoveryExports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure snake_case naming convention for all entities
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Convert table names to snake_case (already done via ToTable)
            // Convert column names to snake_case
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            // Convert foreign key names to snake_case
            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName() ?? ""));
            }

            // Convert index names to snake_case
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName() ?? ""));
            }
        }

        // ============================================================
        // CORE / MULTI-TENANCY SCHEMA
        // ============================================================

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants", "core");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", "core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => new { e.TenantId, e.Username }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PasswordSalt).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
            entity.Property(e => e.LastLoginAt).HasColumnType("timestamptz");
            entity.Property(e => e.LockedUntil).HasColumnType("timestamptz");

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles", "core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IsSystemRole);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.PermissionsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles", "core");
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.RoleId);
            entity.HasIndex(e => e.AssignedAt);

            entity.Property(e => e.AssignedAt).HasColumnType("timestamptz").IsRequired();
            entity.Property(e => e.AssignedBy).IsRequired().HasMaxLength(200);

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================================================
        // CONNECTIONS SCHEMA
        // ============================================================

        modelBuilder.Entity<Connection>(entity =>
        {
            entity.ToTable("connections", "connections");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.Type });
            entity.HasIndex(e => new { e.TenantId, e.Role });
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.EndpointUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AuthenticationMode).HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
            entity.Property(e => e.LastVerifiedAt).HasColumnType("timestamptz");

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Connections)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Secret)
                .WithOne(s => s.Connection)
                .HasForeignKey<ConnectionSecret>(s => s.ConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConnectionSecret>(entity =>
        {
            entity.ToTable("connection_secrets", "connections");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ConnectionId).IsUnique();

            entity.Property(e => e.EncryptedSecret).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        });

        modelBuilder.Entity<ConnectionVerificationRun>(entity =>
        {
            entity.ToTable("connection_verification_runs", "connections");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.ConnectionId, e.StartedAt });
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => e.StartedAt);

            entity.Property(e => e.StartedAt).HasColumnType("timestamptz").IsRequired();
            entity.Property(e => e.CompletedAt).HasColumnType("timestamptz");
            entity.Property(e => e.DiagnosticsJson).HasColumnType("jsonb");

            entity.HasOne(e => e.Connection)
                .WithMany(c => c.VerificationRuns)
                .HasForeignKey(e => e.ConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================================================
        // MIGRATION SCHEMA
        // ============================================================

        modelBuilder.Entity<MigrationPlan>(entity =>
        {
            entity.ToTable("migration_plans", "jobs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ConfigurationJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.MappingRulesJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamptz");

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.MigrationPlans)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SourceConnection)
                .WithMany()
                .HasForeignKey(e => e.SourceConnectionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TargetConnection)
                .WithMany()
                .HasForeignKey(e => e.TargetConnectionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MigrationJob>(entity =>
        {
            entity.ToTable("migration_jobs", "jobs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.Type });
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.StartedAt);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ConfigurationJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.DiagnosticsJson).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
            entity.Property(e => e.StartedAt).HasColumnType("timestamptz");
            entity.Property(e => e.CompletedAt).HasColumnType("timestamptz");
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamptz");

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.MigrationJobs)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.MigrationPlan)
                .WithMany(p => p.MigrationJobs)
                .HasForeignKey(e => e.MigrationPlanId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.SourceConnection)
                .WithMany()
                .HasForeignKey(e => e.SourceConnectionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TargetConnection)
                .WithMany()
                .HasForeignKey(e => e.TargetConnectionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MigrationTask>(entity =>
        {
            entity.ToTable("migration_tasks", "jobs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => new { e.MigrationJobId, e.Status });
            entity.HasIndex(e => new { e.MigrationJobId, e.Sequence });
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.SourcePath).HasMaxLength(2000);
            entity.Property(e => e.TargetPath).HasMaxLength(2000);
            entity.Property(e => e.TaskDataJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
            entity.Property(e => e.StartedAt).HasColumnType("timestamptz");
            entity.Property(e => e.CompletedAt).HasColumnType("timestamptz");

            entity.HasOne(e => e.MigrationJob)
                .WithMany(j => j.Tasks)
                .HasForeignKey(e => e.MigrationJobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskCheckpoint>(entity =>
        {
            entity.ToTable("task_checkpoints", "jobs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => new { e.MigrationTaskId, e.CreatedAt });

            entity.Property(e => e.CheckpointType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CheckpointData).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();

            entity.HasOne(e => e.MigrationTask)
                .WithMany(t => t.Checkpoints)
                .HasForeignKey(e => e.MigrationTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================================================
        // VALIDATION & REPORTING SCHEMA
        // ============================================================

        modelBuilder.Entity<ValidationRun>(entity =>
        {
            entity.ToTable("validation_runs", "validation");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.Type });
            entity.HasIndex(e => e.StartedAt);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ParametersJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.StartedAt).HasColumnType("timestamptz").IsRequired();
            entity.Property(e => e.CompletedAt).HasColumnType("timestamptz");

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.ValidationRuns)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.MigrationJob)
                .WithMany(j => j.ValidationRuns)
                .HasForeignKey(e => e.MigrationJobId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.SourceConnection)
                .WithMany()
                .HasForeignKey(e => e.SourceConnectionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TargetConnection)
                .WithMany()
                .HasForeignKey(e => e.TargetConnectionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ValidationFinding>(entity =>
        {
            entity.ToTable("validation_findings", "validation");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => new { e.ValidationRunId, e.Severity });
            entity.HasIndex(e => new { e.ValidationRunId, e.Category });
            entity.HasIndex(e => e.DetectedAt);

            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.SourcePath).HasMaxLength(2000);
            entity.Property(e => e.TargetPath).HasMaxLength(2000);
            entity.Property(e => e.DetailsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.RecommendedAction).HasMaxLength(1000);
            entity.Property(e => e.DetectedAt).HasColumnType("timestamptz").IsRequired();

            entity.HasOne(e => e.ValidationRun)
                .WithMany(v => v.Findings)
                .HasForeignKey(e => e.ValidationRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.ToTable("reports", "reporting");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => new { e.TenantId, e.Type });
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => e.GeneratedAt);
            entity.HasIndex(e => e.ExpiresAt);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ParametersJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.GeneratedAt).HasColumnType("timestamptz").IsRequired();
            entity.Property(e => e.ExpiresAt).HasColumnType("timestamptz");

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Reports)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.MigrationJob)
                .WithMany(j => j.Reports)
                .HasForeignKey(e => e.MigrationJobId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ValidationRun)
                .WithMany()
                .HasForeignKey(e => e.ValidationRunId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ReportFile>(entity =>
        {
            entity.ToTable("report_files", "reporting");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ContentHash).HasMaxLength(128);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();

            entity.HasOne(e => e.Report)
                .WithMany(r => r.ReportFiles)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================================================
        // AUDITING & OBSERVABILITY SCHEMA
        // ============================================================

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("audit_events", "audit");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => new { e.TenantId, e.Timestamp });
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);

            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OldValuesJson).HasColumnType("jsonb");
            entity.Property(e => e.NewValuesJson).HasColumnType("jsonb");
            entity.Property(e => e.MetadataJson).HasColumnType("jsonb");
            entity.Property(e => e.Username).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IpAddress).HasMaxLength(45); // IPv6
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Timestamp).HasColumnType("timestamptz").IsRequired();

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.AuditEvents)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditEvents)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<JobLog>(entity =>
        {
            entity.ToTable("job_logs", "audit");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => new { e.MigrationJobId, e.Timestamp });
            entity.HasIndex(e => new { e.MigrationJobId, e.Level });
            entity.HasIndex(e => e.MigrationTaskId);
            entity.HasIndex(e => e.Timestamp);

            entity.Property(e => e.Message).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.DetailsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Timestamp).HasColumnType("timestamptz").IsRequired();

            entity.HasOne(e => e.MigrationJob)
                .WithMany(j => j.JobLogs)
                .HasForeignKey(e => e.MigrationJobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.MigrationTask)
                .WithMany(t => t.JobLogs)
                .HasForeignKey(e => e.MigrationTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================================================
        // DISCOVERY SCHEMA
        // ============================================================

        modelBuilder.Entity<DiscoveryRun>(entity =>
        {
            entity.ToTable("discovery_runs", "discovery");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.SourceConnectionId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.TenantId, e.Status });

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ScopeUrl).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.ConfigurationJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CurrentStep).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(200);
            entity.Property(e => e.StartedAt).HasColumnType("timestamptz");
            entity.Property(e => e.CompletedAt).HasColumnType("timestamptz");
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
            entity.Property(e => e.ArchivedAt).HasColumnType("timestamptz");

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SourceConnection)
                .WithMany()
                .HasForeignKey(e => e.SourceConnectionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DiscoveryItem>(entity =>
        {
            entity.ToTable("discovery_items", "discovery");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => e.DiscoveryRunId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.ParentItemId);
            entity.HasIndex(e => e.ItemType);
            entity.HasIndex(e => e.Path);
            entity.HasIndex(e => new { e.DiscoveryRunId, e.ItemType });
            entity.HasIndex(e => new { e.TenantId, e.DiscoveryRunId });

            entity.Property(e => e.Path).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.InternalName).HasMaxLength(500);
            entity.Property(e => e.ContentTypesJson).HasColumnType("jsonb");
            entity.Property(e => e.ColumnsJson).HasColumnType("jsonb");
            entity.Property(e => e.TemplateType).HasMaxLength(100);
            entity.Property(e => e.MetadataJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CreatedDate).HasColumnType("timestamptz");
            entity.Property(e => e.ModifiedDate).HasColumnType("timestamptz");
            entity.Property(e => e.DiscoveredAt).HasColumnType("timestamptz").IsRequired();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DiscoveryRun)
                .WithMany(r => r.Items)
                .HasForeignKey(e => e.DiscoveryRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParentItem)
                .WithMany(p => p.ChildItems)
                .HasForeignKey(e => e.ParentItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DiscoveryMetric>(entity =>
        {
            entity.ToTable("discovery_metrics", "discovery");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => e.DiscoveryRunId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.MetricKey);
            entity.HasIndex(e => new { e.DiscoveryRunId, e.MetricKey });

            entity.Property(e => e.MetricKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MetricCategory).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StringValue).HasMaxLength(500);
            entity.Property(e => e.JsonValue).HasColumnType("jsonb");
            entity.Property(e => e.CalculatedAt).HasColumnType("timestamptz").IsRequired();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DiscoveryRun)
                .WithMany(r => r.Metrics)
                .HasForeignKey(e => e.DiscoveryRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DiscoveryWarning>(entity =>
        {
            entity.ToTable("discovery_warnings", "discovery");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => e.DiscoveryRunId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.DiscoveryItemId);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => new { e.DiscoveryRunId, e.Severity });

            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.DetailedMessage).HasMaxLength(4000);
            entity.Property(e => e.ItemPath).HasMaxLength(2000);
            entity.Property(e => e.RecommendationJson).HasColumnType("jsonb");
            entity.Property(e => e.DetectedAt).HasColumnType("timestamptz").IsRequired();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DiscoveryRun)
                .WithMany(r => r.Warnings)
                .HasForeignKey(e => e.DiscoveryRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.DiscoveryItem)
                .WithMany()
                .HasForeignKey(e => e.DiscoveryItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DiscoveryExport>(entity =>
        {
            entity.ToTable("discovery_exports", "discovery");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasIndex(e => e.DiscoveryRunId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ExpiresAt);

            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ExpiresAt).HasColumnType("timestamptz");

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DiscoveryRun)
                .WithMany(r => r.Exports)
                .HasForeignKey(e => e.DiscoveryRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // Helper method to convert PascalCase to snake_case
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return Regex.Replace(
            Regex.Replace(input, @"([A-Z])([A-Z][a-z])", "$1_$2"),
            @"([a-z\d])([A-Z])", "$1_$2"
        ).ToLower();
    }
}
