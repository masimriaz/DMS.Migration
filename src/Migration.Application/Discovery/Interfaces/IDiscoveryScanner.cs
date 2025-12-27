using DMS.Migration.Domain.Entities;

namespace DMS.Migration.Application.Discovery.Interfaces;

/// <summary>
/// Scanner for discovering SharePoint content
/// </summary>
public interface IDiscoveryScanner
{
    /// <summary>
    /// Scans the SharePoint environment and yields discovered items
    /// </summary>
    IAsyncEnumerable<DiscoveryItem> ScanAsync(
        Connection connection,
        DiscoveryRun run,
        string scopeUrl,
        Dictionary<string, object> configuration,
        CancellationToken cancellationToken = default);
}
