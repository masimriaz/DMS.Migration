using DMS.Migration.Domain.Entities;

namespace DMS.Migration.Application.Interfaces;

/// <summary>
/// Scanner abstraction for discovering SharePoint content
/// </summary>
public interface IDiscoveryScanner
{
    /// <summary>
    /// Execute the discovery scan
    /// </summary>
    Task ExecuteScanAsync(DiscoveryRun run, Connection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that the connection is accessible
    /// </summary>
    Task<bool> ValidateConnectionAsync(Connection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test connection and get basic info
    /// </summary>
    Task<Dictionary<string, object>> GetConnectionInfoAsync(Connection connection, CancellationToken cancellationToken = default);
}
