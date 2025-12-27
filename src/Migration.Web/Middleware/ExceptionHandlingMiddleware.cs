using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using DMS.Migration.Application.Common.Models;

namespace DMS.Migration.Web.Middleware;

/// <summary>
/// Global exception handler middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        _logger.LogError(
            exception,
            "Unhandled exception occurred. CorrelationId: {CorrelationId}",
            correlationId);

        var error = exception switch
        {
            UnauthorizedAccessException => new ErrorResponse(
                "Unauthorized",
                "You are not authorized to perform this action",
                HttpStatusCode.Unauthorized,
                correlationId),

            ArgumentException or InvalidOperationException => new ErrorResponse(
                "BadRequest",
                exception.Message,
                HttpStatusCode.BadRequest,
                correlationId),

            KeyNotFoundException => new ErrorResponse(
                "NotFound",
                exception.Message,
                HttpStatusCode.NotFound,
                correlationId),

            _ => new ErrorResponse(
                "InternalServerError",
                "An error occurred while processing your request",
                HttpStatusCode.InternalServerError,
                correlationId)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)error.StatusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsJsonAsync(error, options);
    }

    private record ErrorResponse(
        string Code,
        string Message,
        HttpStatusCode StatusCode,
        string CorrelationId);
}
