using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DMS.Migration.Web.Middleware;

/// <summary>
/// Middleware to add a correlation ID to each request for distributed tracing
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get correlation ID from request header, or generate a new one
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                          ?? Guid.NewGuid().ToString();

        // Store in HttpContext.Items for access by services
        context.Items["CorrelationId"] = correlationId;

        // Add to response headers
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Add to logging scope
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }
}
