using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Application.Discovery.Interfaces;

/// <summary>
/// Repository for Discovery operations
/// </summary>
public interface IDiscoveryRepository
{
    Task<DiscoveryRun?> GetRunByIdAsync(Guid runId, int tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscoveryRun>> GetRunsAsync(
        int tenantId,
        DiscoveryStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<DiscoveryRun> CreateRunAsync(DiscoveryRun run, CancellationToken cancellationToken = default);

    Task UpdateRunAsync(DiscoveryRun run, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscoveryItem>> GetItemsAsync(
        Guid runId,
        int tenantId,
        CancellationToken cancellationToken = default);

    Task AddItemsAsync(IEnumerable<DiscoveryItem> items, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscoveryWarning>> GetWarningsAsync(
        Guid runId,
        int tenantId,
        CancellationToken cancellationToken = default);
}
