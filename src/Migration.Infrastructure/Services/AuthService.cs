using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DMS.Migration.Application.Interfaces;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Infrastructure.Data;
using System.Security.Claims;

namespace DMS.Migration.Infrastructure.Services;

/// <summary>
/// Authentication service using ASP.NET Core Identity PasswordHasher.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext context,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
        _logger = logger;
    }

    public async Task<User?> ValidateCredentialsAsync(string email, string password)
    {
        try
        {
            // Find user by email (case-insensitive)
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                _logger.LogWarning("Login attempt for non-existent email: {Email}", email);
                return null;
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive user: {Email}", email);
                return null;
            }

            // Check if user is locked
            if (user.IsLocked && user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("Login attempt for locked user: {Email} (locked until {LockedUntil})",
                    email, user.LockedUntil.Value);
                return null;
            }

            // Verify password using PasswordHasher (constant-time comparison)
            var verificationResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Invalid password for user: {Email}", email);

                // Increment failed login attempts (optional, for future lockout implementation)
                user.FailedLoginAttempts++;
                await _context.SaveChangesAsync();

                return null;
            }

            // Password verified successfully
            _logger.LogInformation("User authenticated successfully: {Email}", email);

            // Reset failed login attempts
            if (user.FailedLoginAttempts > 0)
            {
                user.FailedLoginAttempts = 0;
                await _context.SaveChangesAsync();
            }

            // Note: If result is SuccessRehashNeeded, you can optionally rehash with newer algorithm
            // For now, we'll accept both Success and SuccessRehashNeeded

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during credential validation for email: {Email}", email);
            return null;
        }
    }

    public ClaimsPrincipal BuildClaimsPrincipal(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("FullName", user.FullName),
            new Claim("TenantId", user.TenantId.ToString()),
            new Claim("TenantName", user.Tenant?.Name ?? "Unknown")
        };

        // Add role claims
        foreach (var userRole in user.UserRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Code));
            claims.Add(new Claim("RoleName", userRole.Role.Name));
        }

        var identity = new ClaimsIdentity(claims, "DMS.Migration.Auth");
        return new ClaimsPrincipal(identity);
    }

    public async Task UpdateLastLoginAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated last login for user: {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user: {UserId}", userId);
        }
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}
