using Microsoft.AspNetCore.Http;
using DMS.Migration.Application.Common.Abstractions;

namespace DMS.Migration.Infrastructure.Services;

/// <summary>
/// Provides tenant context from HTTP session (demo implementation)
/// In production, this would read from JWT claims or authenticated user
/// </summary>
public class TenantContextService : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const int DefaultTenantId = 1; // Demo fallback

    public TenantContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int TenantId
    {
        get
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null && session.TryGetValue("TenantId", out var tenantBytes))
            {
                return BitConverter.ToInt32(tenantBytes, 0);
            }
            return DefaultTenantId;
        }
    }

    public string CurrentUser
    {
        get
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var username = session?.GetString("Username");
            return username ?? "System";
        }
    }

    public string CorrelationId
    {
        get
        {
            // Will be set by CorrelationIdMiddleware
            var context = _httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue("CorrelationId", out var correlationId) == true)
            {
                return correlationId?.ToString() ?? Guid.NewGuid().ToString();
            }
            return Guid.NewGuid().ToString();
        }
    }
}
