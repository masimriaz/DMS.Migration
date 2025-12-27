using Microsoft.Extensions.Logging;
using DMS.Migration.Application.Jobs.Interfaces;

namespace DMS.Migration.Infrastructure.Jobs;

/// <summary>
/// Retry policy with exponential backoff for job operations
/// </summary>
public class RetryPolicy : IRetryPolicy
{
    private readonly ILogger<RetryPolicy> _logger;

    public RetryPolicy(ILogger<RetryPolicy> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var maxRetries = 3;
        var retryCount = 0;

        while (true)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (retryCount < maxRetries && !(ex is OperationCanceledException))
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));

                _logger.LogWarning(
                    ex,
                    "Retry {RetryCount}/{MaxRetries} after {Delay}s for operation: {Operation}",
                    retryCount,
                    maxRetries,
                    delay.TotalSeconds,
                    operationName);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
