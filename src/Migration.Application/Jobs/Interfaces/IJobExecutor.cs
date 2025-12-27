namespace DMS.Migration.Application.Jobs.Interfaces;

/// <summary>
/// Executes a background job
/// </summary>
public interface IJobExecutor<TJobData>
{
    Task ExecuteAsync(TJobData jobData, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a single step in a job execution pipeline
/// </summary>
public interface IJobStep<TContext>
{
    string StepName { get; }
    Task<TContext> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Retry policy for job execution
/// </summary>
public interface IRetryPolicy
{
    Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default);
}
