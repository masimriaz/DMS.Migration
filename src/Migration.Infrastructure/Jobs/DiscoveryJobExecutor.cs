using Microsoft.Extensions.Logging;
using DMS.Migration.Application.Jobs.Interfaces;
using DMS.Migration.Application.Discovery.Interfaces;
using DMS.Migration.Application.Connections.Interfaces;
using DMS.Migration.Domain.Enums;
using System.Text.Json;

namespace DMS.Migration.Infrastructure.Jobs;

/// <summary>
/// Background job payload for discovery runs
/// </summary>
public class DiscoveryJob
{
    public Guid DiscoveryRunId { get; set; }
    public int TenantId { get; set; }
}

/// <summary>
/// Executor for discovery background jobs
/// </summary>
public class DiscoveryJobExecutor : IJobExecutor<DiscoveryJob>
{
    private readonly IDiscoveryRepository _discoveryRepository;
    private readonly IConnectionRepository _connectionRepository;
    private readonly IDiscoveryScanner _scanner;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ILogger<DiscoveryJobExecutor> _logger;

    public DiscoveryJobExecutor(
        IDiscoveryRepository discoveryRepository,
        IConnectionRepository connectionRepository,
        IDiscoveryScanner scanner,
        IRetryPolicy retryPolicy,
        ILogger<DiscoveryJobExecutor> logger)
    {
        _discoveryRepository = discoveryRepository;
        _connectionRepository = connectionRepository;
        _scanner = scanner;
        _retryPolicy = retryPolicy;
        _logger = logger;
    }

    public async Task ExecuteAsync(DiscoveryJob jobData, CancellationToken cancellationToken = default)
    {
        var run = await _discoveryRepository.GetRunByIdAsync(jobData.DiscoveryRunId, jobData.TenantId, cancellationToken);
        if (run == null)
        {
            _logger.LogError("Discovery run {RunId} not found", jobData.DiscoveryRunId);
            return;
        }

        try
        {
            // Update status to running
            run.Status = DiscoveryStatus.Running;
            run.StartedAt = DateTime.UtcNow;
            await _discoveryRepository.UpdateRunAsync(run, cancellationToken);

            _logger.LogInformation("Starting discovery run {RunId} for tenant {TenantId}", run.Id, run.TenantId);

            // Get connection
            var connection = await _connectionRepository.GetByIdAsync(
                run.SourceConnectionId,
                run.TenantId,
                cancellationToken);

            if (connection == null)
            {
                throw new InvalidOperationException($"Connection {run.SourceConnectionId} not found");
            }

            // Parse configuration
            var config = string.IsNullOrWhiteSpace(run.ConfigurationJson)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(run.ConfigurationJson) ?? new();

            // Execute discovery with batching
            var batchSize = 100;
            var batch = new List<Domain.Entities.DiscoveryItem>(batchSize);
            int itemCount = 0;

            await foreach (var item in _scanner.ScanAsync(connection, run, run.ScopeUrl, config, cancellationToken))
            {
                batch.Add(item);
                itemCount++;

                if (batch.Count >= batchSize)
                {
                    await _discoveryRepository.AddItemsAsync(batch, cancellationToken);
                    _logger.LogInformation("Processed batch of {Count} items (total: {Total})", batch.Count, itemCount);
                    batch.Clear();
                }
            }

            // Save remaining items
            if (batch.Count > 0)
            {
                await _discoveryRepository.AddItemsAsync(batch, cancellationToken);
            }

            // Update run with results
            run.Status = DiscoveryStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            run.TotalItemsScanned = itemCount;
            // Note: TotalSizeBytes not in entity, can be calculated from items if needed
            await _discoveryRepository.UpdateRunAsync(run, cancellationToken);

            _logger.LogInformation(
                "Discovery run {RunId} completed. Found {ItemCount} items",
                run.Id, itemCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Discovery run {RunId} failed: {Error}", run.Id, ex.Message);

            run.Status = DiscoveryStatus.Failed;
            run.ErrorMessage = ex.Message;
            run.CompletedAt = DateTime.UtcNow;
            await _discoveryRepository.UpdateRunAsync(run, cancellationToken);
        }
    }
}
