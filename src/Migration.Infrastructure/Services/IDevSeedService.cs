using System.Threading.Tasks;

namespace DMS.Migration.Infrastructure.Services;

/// <summary>
/// Service for seeding development data.
/// WARNING: Only use in development environment!
/// </summary>
public interface IDevSeedService
{
    /// <summary>
    /// Seeds development users, roles, and tenants.
    /// Only executes in Development environment.
    /// </summary>
    Task SeedDevelopmentDataAsync();
}
