using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DMS.Migration.Application.Interfaces;

namespace DMS.Migration.Infrastructure.Jobs;

// Simple in-memory queue for all job types
public class ChannelJobQueue : IJobQueue
{
    private readonly Channel<(string JobType, string JobData)> _queue;

    public ChannelJobQueue()
    {
        _queue = Channel.CreateUnbounded<(string, string)>();
    }

    // Legacy method for migration jobs
    public async Task EnqueueJobAsync(Guid jobId, CancellationToken ct = default)
    {
        await EnqueueAsync("migration", jobId.ToString(), ct);
    }

    // Legacy method for migration jobs
    public async Task<Guid?> DequeueJobAsync(CancellationToken ct = default)
    {
        var (jobType, jobData) = await DequeueAsync(ct);
        if (jobType == "migration" && Guid.TryParse(jobData, out var jobId))
            return jobId;
        return null;
    }

    // New typed methods
    public async Task EnqueueAsync(string jobType, string jobData, CancellationToken ct = default)
    {
        await _queue.Writer.WriteAsync((jobType, jobData), ct);
    }

    public async Task<(string JobType, string JobData)> DequeueAsync(CancellationToken ct = default)
    {
        return await _queue.Reader.ReadAsync(ct);
    }
}
