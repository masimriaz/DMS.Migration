namespace DMS.Migration.Application.Common.Abstractions;

/// <summary>
/// Provides access to the current tenant context for multi-tenant isolation
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID from the execution context (session, claims, etc.)
    /// </summary>
    int TenantId { get; }

    /// <summary>
    /// Gets the current user identifier
    /// </summary>
    string CurrentUser { get; }

    /// <summary>
    /// Gets the correlation ID for the current request (for tracing)
    /// </summary>
    string CorrelationId { get; }
}
