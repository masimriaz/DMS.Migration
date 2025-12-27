using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting database seeding...");

            // Create Schemas (if not using migrations to create them)
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE SCHEMA IF NOT EXISTS core;
                CREATE SCHEMA IF NOT EXISTS connections;
                CREATE SCHEMA IF NOT EXISTS jobs;
                CREATE SCHEMA IF NOT EXISTS validation;
                CREATE SCHEMA IF NOT EXISTS reporting;
                CREATE SCHEMA IF NOT EXISTS audit;
            ");

            // Seed Tenant
            if (!await context.Tenants.AnyAsync())
            {
                logger.LogInformation("Seeding tenant...");

                var tenant = new Tenant
                {
                    Id = 1,
                    Name = "Demo Corporation",
                    Code = "DEMO",
                    Description = "Demonstration tenant for SharePoint migration tool",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System"
                };

                context.Tenants.Add(tenant);
                await context.SaveChangesAsync();
                logger.LogInformation("✓ Tenant created: {TenantName}", tenant.Name);
            }

            // Seed Roles
            if (!await context.Roles.AnyAsync())
            {
                logger.LogInformation("Seeding roles...");

                var roles = new[]
                {
                    new Role
                    {
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Name = "Administrator",
                        Code = "ADMIN",
                        Description = "Full system access with all permissions",
                        IsSystemRole = true,
                        Priority = 100,
                        PermissionsJson = @"[""*""]",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Role
                    {
                        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        Name = "Migration Operator",
                        Code = "OPERATOR",
                        Description = "Can create and manage migrations",
                        IsSystemRole = true,
                        Priority = 50,
                        PermissionsJson = @"[""connections.view"", ""connections.create"", ""migrations.view"", ""migrations.create"", ""migrations.execute""]",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Role
                    {
                        Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                        Name = "Viewer",
                        Code = "VIEWER",
                        Description = "Read-only access to view migrations and reports",
                        IsSystemRole = true,
                        Priority = 10,
                        PermissionsJson = @"[""connections.view"", ""migrations.view"", ""reports.view""]",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Roles.AddRange(roles);
                await context.SaveChangesAsync();
                logger.LogInformation("✓ {Count} roles created", roles.Length);
            }

            // Seed Admin User
            if (!await context.Users.AnyAsync())
            {
                logger.LogInformation("Seeding admin user...");

                var adminUser = new User
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    TenantId = 1,
                    Username = "admin",
                    Email = "admin@democorp.com",
                    FullName = "System Administrator",
                    PasswordHash = "demo-hash-change-in-production", // In production, use proper password hashing
                    IsActive = true,
                    IsLocked = false,
                    FailedLoginAttempts = 0,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();

                // Assign admin role
                var adminRole = await context.Roles.FirstAsync(r => r.Code == "ADMIN");
                var userRole = new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = "System"
                };

                context.UserRoles.Add(userRole);
                await context.SaveChangesAsync();

                logger.LogInformation("✓ Admin user created: {Username}", adminUser.Username);
            }

            // Seed Sample Connections
            if (!await context.Connections.AnyAsync())
            {
                logger.LogInformation("Seeding sample connections...");

                var connections = new[]
                {
                    new Connection
                    {
                        Id = 1,
                        TenantId = 1,
                        Name = "SharePoint 2016 Source",
                        Description = "Legacy SharePoint 2016 on-premises environment",
                        Role = ConnectionRole.Source,
                        Type = ConnectionType.SharePointOnPrem,
                        Status = ConnectionStatus.Draft,
                        EndpointUrl = "https://sharepoint2016.democorp.local",
                        AuthenticationMode = "ServiceAccount",
                        Username = "sp_migration@democorp.local",
                        ThrottlingProfile = ThrottlingProfile.Normal,
                        PreserveAuthorship = true,
                        PreserveTimestamps = true,
                        ReplaceIllegalCharacters = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "admin",
                        IsDeleted = false
                    },
                    new Connection
                    {
                        Id = 2,
                        TenantId = 1,
                        Name = "SharePoint Online Target",
                        Description = "Modern SharePoint Online tenant",
                        Role = ConnectionRole.Target,
                        Type = ConnectionType.SharePointOnline,
                        Status = ConnectionStatus.Draft,
                        EndpointUrl = "https://democorp.sharepoint.com",
                        AuthenticationMode = "AppOnly",
                        Username = "app@democorp.onmicrosoft.com",
                        ThrottlingProfile = ThrottlingProfile.Normal,
                        PreserveAuthorship = true,
                        PreserveTimestamps = true,
                        ReplaceIllegalCharacters = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "admin",
                        IsDeleted = false
                    }
                };

                context.Connections.AddRange(connections);
                await context.SaveChangesAsync();
                logger.LogInformation("✓ {Count} sample connections created", connections.Length);
            }

            // Seed Sample Migration Plan
            if (!await context.MigrationPlans.AnyAsync())
            {
                logger.LogInformation("Seeding sample migration plan...");

                var sourceConnection = await context.Connections.FirstAsync(c => c.Role == ConnectionRole.Source);
                var targetConnection = await context.Connections.FirstAsync(c => c.Role == ConnectionRole.Target);

                var plan = new MigrationPlan
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    TenantId = 1,
                    Name = "Legacy to Modern Migration",
                    Description = "Migrate from SharePoint 2016 to SharePoint Online",
                    SourceConnectionId = sourceConnection.Id,
                    TargetConnectionId = targetConnection.Id,
                    Status = PlanStatus.Draft,
                    ConfigurationJson = @"{
                        ""preserveVersionHistory"": true,
                        ""migratePermissions"": true,
                        ""migrateWorkflows"": false,
                        ""maxFileSizeMB"": 15000,
                        ""parallelism"": 5
                    }",
                    MappingRulesJson = @"{
                        ""siteMapping"": [
                            {""source"": ""/sites/legacy"", ""target"": ""/sites/modern""}
                        ],
                        ""userMapping"": []
                    }",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "admin"
                };

                context.MigrationPlans.Add(plan);
                await context.SaveChangesAsync();
                logger.LogInformation("✓ Sample migration plan created: {PlanName}", plan.Name);
            }

            // Seed Sample Migration Job (Draft)
            if (!await context.MigrationJobs.AnyAsync())
            {
                logger.LogInformation("Seeding sample migration job...");

                var plan = await context.MigrationPlans.FirstAsync();
                var sourceConnection = await context.Connections.FirstAsync(c => c.Role == ConnectionRole.Source);
                var targetConnection = await context.Connections.FirstAsync(c => c.Role == ConnectionRole.Target);

                var job = new MigrationJob
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    TenantId = 1,
                    MigrationPlanId = plan.Id,
                    Name = "Demo Migration Job - Site Collection Alpha",
                    Description = "Initial test migration of site collection /sites/alpha",
                    Type = JobType.Migration,
                    Status = JobStatus.Draft,
                    SourceConnectionId = sourceConnection.Id,
                    TargetConnectionId = targetConnection.Id,
                    ConfigurationJson = @"{
                        ""sourceSiteUrl"": ""/sites/alpha"",
                        ""targetSiteUrl"": ""/sites/alpha-migrated"",
                        ""includeSubsites"": true,
                        ""includeDocumentLibraries"": true,
                        ""includeListData"": true
                    }",
                    TotalItems = 0,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    SkippedItems = 0,
                    TotalBytes = 0,
                    ProcessedBytes = 0,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "admin"
                };

                context.MigrationJobs.Add(job);
                await context.SaveChangesAsync();
                logger.LogInformation("✓ Sample migration job created: {JobName}", job.Name);
            }

            // Seed Audit Event
            if (!await context.AuditEvents.AnyAsync())
            {
                logger.LogInformation("Seeding initial audit event...");

                var adminUser = await context.Users.FirstAsync(u => u.Username == "admin");

                var auditEvent = new AuditEvent
                {
                    Id = Guid.NewGuid(),
                    TenantId = 1,
                    EntityType = "Database",
                    EntityId = "System",
                    Action = "Seed",
                    NewValuesJson = @"{""message"": ""Initial database seeding completed""}",
                    MetadataJson = @"{""environment"": ""Development""}",
                    Timestamp = DateTime.UtcNow,
                    UserId = adminUser.Id,
                    Username = adminUser.Username,
                    IpAddress = "127.0.0.1",
                    UserAgent = "DatabaseSeeder/1.0"
                };

                context.AuditEvents.Add(auditEvent);
                await context.SaveChangesAsync();
                logger.LogInformation("✓ Initial audit event created");
            }

            logger.LogInformation("✅ Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error during database seeding: {Message}", ex.Message);
            throw;
        }
    }
}
