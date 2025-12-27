using DMS.Migration.Domain.Entities;

namespace DMS.Migration.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(int tenantId, string entityType, int entityId, string action, string userId, string username, string? oldValues = null, string? newValues = null);
}
