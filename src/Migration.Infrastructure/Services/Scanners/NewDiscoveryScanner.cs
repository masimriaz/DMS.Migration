using DMS.Migration.Application.Discovery.Interfaces;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace DMS.Migration.Infrastructure.Services.Scanners;

/// <summary>
/// Adapter that implements new IDiscoveryScanner using old scanner infrastructure
/// </summary>
public class NewDiscoveryScanner : IDiscoveryScanner
{
    private readonly IServiceProvider _serviceProvider;

    public NewDiscoveryScanner(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async IAsyncEnumerable<DiscoveryItem> ScanAsync(
        Connection connection,
        DiscoveryRun run,
        string scopeUrl,
        Dictionary<string, object> configuration,
        CancellationToken cancellationToken = default)
    {
        // Get the old-style scanner
        var oldScanner = _serviceProvider.GetRequiredService<Application.Interfaces.IDiscoveryScanner>();

        // Call old scanner (which doesn't return items directly, saves to DB)
        await oldScanner.ExecuteScanAsync(run, connection, cancellationToken);

        // Return empty enumerable for now (old scanner saves directly to DB)
        yield break;
    }
}
