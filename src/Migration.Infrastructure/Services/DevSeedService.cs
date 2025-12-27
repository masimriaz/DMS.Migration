using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Infrastructure.Data;

namespace DMS.Migration.Infrastructure.Services;

/// <summary>
/// Development seed service for creating test users and data.
/// WARNING: Contains hardcoded credentials - DEVELOPMENT ONLY!
/// </summary>
public class DevSeedService : IDevSeedService
{
    private readonly AppDbContext _context;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DevSeedService> _logger;
    private readonly PasswordHasher<User> _passwordHasher;

    public DevSeedService(
        AppDbContext context,
        IHostEnvironment environment,
        ILogger<DevSeedService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task SeedDevelopmentDataAsync()
    {
        // CRITICAL: Only run in Development
        if (!_environment.IsDevelopment())
        {
            _logger.LogWarning("⚠️  DevSeedService called in non-Development environment - SKIPPING");
            return;
        }

        try
        {
            _logger.LogInformation("🌱 Seeding development data...");

            // 1. Ensure demo tenant exists
            var demoTenant = await EnsureTenantAsync();

            // 2. Ensure system roles exist
            var (adminRole, operatorRole, viewerRole) = await EnsureRolesAsync();

            // 3. Seed admin user
            var adminUser = await SeedUserAsync(
                id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                username: "admin",
                email: "admin@dms.local",
                fullName: "System Administrator",
                password: "Admin@123",
                tenant: demoTenant,
                role: adminRole);

            // 4. Seed operator user
            var operatorUser = await SeedUserAsync(
                id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                username: "operator",
                email: "operator@dms.local",
                fullName: "Demo Operator",
                password: "Operator@123",
                tenant: demoTenant,
                role: operatorRole);

            _logger.LogInformation("✅ Development data seeded successfully!");
            _logger.LogInformation("");
            _logger.LogInformation("╔════════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║          DEVELOPMENT CREDENTIALS                           ║");
            _logger.LogInformation("╠════════════════════════════════════════════════════════════╣");
            _logger.LogInformation("║  Admin User:                                               ║");
            _logger.LogInformation("║    Email:    admin@dms.local                               ║");
            _logger.LogInformation("║    Password: Admin@123                                     ║");
            _logger.LogInformation("║    Role:     Administrator                                 ║");
            _logger.LogInformation("║                                                            ║");
            _logger.LogInformation("║  Operator User:                                            ║");
            _logger.LogInformation("║    Email:    operator@dms.local                            ║");
            _logger.LogInformation("║    Password: Operator@123                                  ║");
            _logger.LogInformation("║    Role:     Operator                                      ║");
            _logger.LogInformation("╚════════════════════════════════════════════════════════════╝");
            _logger.LogInformation("");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error seeding development data");
            throw;
        }
    }

    private async Task<Tenant> EnsureTenantAsync()
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == 1);

        if (tenant == null)
        {
            tenant = new Tenant
            {
                Id = 1,
                Name = "Demo Tenant",
                Code = "DEMO",
                Description = "Development and testing tenant",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "seed-service"
            };
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();
            _logger.LogInformation("  ✓ Created demo tenant");
        }
        else
        {
            _logger.LogInformation("  ✓ Demo tenant already exists");
        }

        return tenant;
    }

    private async Task<(Role adminRole, Role operatorRole, Role viewerRole)> EnsureRolesAsync()
    {
        var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var operatorRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var viewerRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        // Admin role
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == adminRoleId);
        if (adminRole == null)
        {
            adminRole = new Role
            {
                Id = adminRoleId,
                Name = "Administrator",
                Code = "ADMIN",
                Description = "Full system access",
                IsSystemRole = true,
                Priority = 100,
                PermissionsJson = "[\"*\"]",
                CreatedAt = DateTime.UtcNow
            };
            _context.Roles.Add(adminRole);
            _logger.LogInformation("  ✓ Created Administrator role");
        }

        // Operator role
        var operatorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == operatorRoleId);
        if (operatorRole == null)
        {
            operatorRole = new Role
            {
                Id = operatorRoleId,
                Name = "Operator",
                Code = "OPERATOR",
                Description = "Can manage migrations and connections",
                IsSystemRole = true,
                Priority = 50,
                PermissionsJson = "[\"connections.*\", \"migrations.*\", \"discovery.*\"]",
                CreatedAt = DateTime.UtcNow
            };
            _context.Roles.Add(operatorRole);
            _logger.LogInformation("  ✓ Created Operator role");
        }

        // Viewer role
        var viewerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == viewerRoleId);
        if (viewerRole == null)
        {
            viewerRole = new Role
            {
                Id = viewerRoleId,
                Name = "Viewer",
                Code = "VIEWER",
                Description = "Read-only access",
                IsSystemRole = true,
                Priority = 10,
                PermissionsJson = "[\"*.read\"]",
                CreatedAt = DateTime.UtcNow
            };
            _context.Roles.Add(viewerRole);
            _logger.LogInformation("  ✓ Created Viewer role");
        }

        await _context.SaveChangesAsync();
        return (adminRole, operatorRole, viewerRole);
    }

    private async Task<User> SeedUserAsync(
        Guid id,
        string username,
        string email,
        string fullName,
        string password,
        Tenant tenant,
        Role role)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            user = new User
            {
                Id = id,
                TenantId = tenant.Id,
                Username = username,
                Email = email,
                FullName = fullName,
                PasswordHash = _passwordHasher.HashPassword(null!, password),
                PasswordSalt = null,
                IsActive = true,
                IsLocked = false,
                FailedLoginAttempts = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "seed-service"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assign role
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = "seed-service"
            };
            _context.Set<UserRole>().Add(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("  ✓ Created user: {Email} with role {Role}", email, role.Name);
        }
        else
        {
            // Update password hash in case it changed
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _context.SaveChangesAsync();
            _logger.LogInformation("  ✓ User already exists: {Email}", email);
        }

        return user;
    }
}
