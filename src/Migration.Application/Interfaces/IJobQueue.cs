using System;
using System.Threading;
using System.Threading.Tasks;

namespace DMS.Migration.Application.Interfaces
{
    public interface IJobQueue
    {
        // Legacy methods for migration jobs
        Task EnqueueJobAsync(Guid jobId, CancellationToken ct = default);
        Task<Guid?> DequeueJobAsync(CancellationToken ct = default);

        // New typed methods for all job types
        Task EnqueueAsync(string jobType, string jobData, CancellationToken ct = default);
        Task<(string JobType, string JobData)> DequeueAsync(CancellationToken ct = default);
    }
}
