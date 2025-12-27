using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Application.Interfaces;

/// <summary>
/// Discovery service for managing discovery runs and retrieving results
/// </summary>
public interface IDiscoveryService
{
    // Run Management
    Task<DiscoveryRun> CreateDiscoveryRunAsync(int tenantId, int sourceConnectionId, string name, string scopeUrl,
        Dictionary<string, object>? configuration, string createdBy, CancellationToken cancellationToken = default);

    Task<DiscoveryRun?> GetDiscoveryRunAsync(Guid runId, int tenantId, CancellationToken cancellationToken = default);

    Task<List<DiscoveryRun>> GetDiscoveryRunsAsync(int tenantId, int? sourceConnectionId = null,
        DiscoveryStatus? status = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<bool> UpdateProgressAsync(Guid runId, int percentage, string currentStep, CancellationToken cancellationToken = default);

    Task<bool> CompleteDiscoveryRunAsync(Guid runId, CancellationToken cancellationToken = default);

    Task<bool> FailDiscoveryRunAsync(Guid runId, string errorMessage, CancellationToken cancellationToken = default);

    Task<bool> CancelDiscoveryRunAsync(Guid runId, CancellationToken cancellationToken = default);

    // Items and Results
    Task<List<DiscoveryItem>> GetDiscoveryItemsAsync(Guid runId, int tenantId,
        DiscoveryItemType? itemType = null, Guid? parentItemId = null,
        int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default);

    Task<DiscoveryItem?> GetDiscoveryItemByIdAsync(Guid itemId, int tenantId, CancellationToken cancellationToken = default);

    Task<int> GetItemCountAsync(Guid runId, DiscoveryItemType? itemType = null, CancellationToken cancellationToken = default);

    // Metrics
    Task<List<DiscoveryMetric>> GetMetricsAsync(Guid runId, int tenantId, string? category = null, CancellationToken cancellationToken = default);

    Task<Dictionary<string, long>> GetSummaryMetricsAsync(Guid runId, int tenantId, CancellationToken cancellationToken = default);

    // Warnings
    Task<List<DiscoveryWarning>> GetWarningsAsync(Guid runId, int tenantId,
        DiscoverySeverity? minSeverity = null, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    Task<int> GetWarningCountAsync(Guid runId, DiscoverySeverity? minSeverity = null, CancellationToken cancellationToken = default);

    // Export
    Task<DiscoveryExport> CreateExportAsync(Guid runId, int tenantId, DiscoveryExportFormat format,
        DiscoveryExportType exportType, string createdBy, CancellationToken cancellationToken = default);

    Task<byte[]> GenerateExportDataAsync(DiscoveryExport export, CancellationToken cancellationToken = default);

    Task<List<DiscoveryExport>> GetExportsAsync(Guid runId, int tenantId, CancellationToken cancellationToken = default);

    // Cleanup
    Task<int> ArchiveOldRunsAsync(int tenantId, int keepLastN, CancellationToken cancellationToken = default);

    Task<int> DeleteExpiredExportsAsync(CancellationToken cancellationToken = default);
}
