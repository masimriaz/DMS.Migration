using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DMS.Migration.Application.Interfaces;
using DMS.Migration.Infrastructure.Data;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Infrastructure.Jobs
{
    public class JobWorker : BackgroundService
    {
        private readonly IJobQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobWorker> _logger;

        public JobWorker(IJobQueue queue, IServiceProvider serviceProvider, ILogger<JobWorker> logger)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Migration Worker Started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Try to dequeue from discovery queue first
                    var (jobType, jobData) = await _queue.DequeueAsync(stoppingToken);

                    if (jobType == "discovery")
                    {
                        await ProcessDiscoveryJobAsync(jobData, stoppingToken);
                        continue;
                    }

                    // Legacy migration job handling
                    if (Guid.TryParse(jobData, out var jobId))
                    {
                        await ProcessMigrationJobAsync(jobId, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing job");
                }
            }
        }

        private async Task ProcessDiscoveryJobAsync(string jobData, CancellationToken cancellationToken)
        {
            try
            {
                var discoveryJob = JsonSerializer.Deserialize<DiscoveryJob>(jobData);
                if (discoveryJob == null)
                {
                    _logger.LogError("Failed to deserialize discovery job data");
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<DiscoveryJobExecutor>();

                await executor.ExecuteAsync(discoveryJob, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing discovery job");
            }
        }

        private async Task ProcessMigrationJobAsync(Guid jobId, CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var job = await db.MigrationJobs.FindAsync(jobId);
            if (job == null) return;

            job.Status = JobStatus.Running;
            job.StartedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            _logger.LogInformation($"Processing Job {jobId}: {job.Name}");

            // SIMULATE WORK (Milestone A stub)
            await Task.Delay(5000, stoppingToken);

            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }
}
