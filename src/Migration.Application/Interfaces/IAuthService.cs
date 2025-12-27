using DMS.Migration.Domain.Entities;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DMS.Migration.Application.Interfaces;

/// <summary>
/// Authentication service for validating user credentials and building claims.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Validates user credentials and returns authenticated user with roles.
    /// </summary>
    /// <param name="email">User email (case-insensitive)</param>
    /// <param name="password">Plain text password to verify</param>
    /// <returns>User entity with roles if valid, null if invalid</returns>
    Task<User?> ValidateCredentialsAsync(string email, string password);

    /// <summary>
    /// Builds a ClaimsPrincipal for the authenticated user.
    /// </summary>
    /// <param name="user">Authenticated user entity</param>
    /// <returns>ClaimsPrincipal with UserId, Email, TenantId, and Role claims</returns>
    ClaimsPrincipal BuildClaimsPrincipal(User user);

    /// <summary>
    /// Updates user's last login timestamp.
    /// </summary>
    Task UpdateLastLoginAsync(Guid userId);

    /// <summary>
    /// Gets user by ID with roles loaded.
    /// </summary>
    Task<User?> GetUserByIdAsync(Guid userId);
}
