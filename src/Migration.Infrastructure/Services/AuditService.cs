using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Infrastructure.Data;

namespace DMS.Migration.Infrastructure.Services;

public class AuditService : Application.Interfaces.IAuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(AppDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(
        int tenantId,
        string entityType,
        int entityId,
        string action,
        string userId,
        string username,
        string? oldValues = null,
        string? newValues = null)
    {
        try
        {
            var auditEvent = new AuditEvent
            {
                TenantId = tenantId,
                EntityType = entityType,
                EntityId = entityId.ToString(),
                Action = action,
                UserId = string.IsNullOrEmpty(userId) ? null : Guid.TryParse(userId, out var uid) ? uid : null,
                Username = username,
                OldValuesJson = oldValues,
                NewValuesJson = newValues,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditEvents.Add(auditEvent);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Audit event logged: {EntityType} {EntityId} - {Action}", entityType, entityId, action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event for {EntityType} {EntityId}", entityType, entityId);
            // Don't throw - auditing should not break the main flow
        }
    }
}
