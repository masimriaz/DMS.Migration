using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Application.Interfaces;

public interface IConnectionService
{
    Task<IEnumerable<Connection>> GetAllAsync(int tenantId, ConnectionRole? role = null, ConnectionType? type = null, ConnectionStatus? status = null, string? searchTerm = null);
    Task<Connection?> GetByIdAsync(int id, int tenantId);
    Task<Connection> CreateAsync(Connection connection, string? password = null);
    Task<Connection> UpdateAsync(Connection connection, string? password = null);
    Task<bool> DeleteAsync(int id, int tenantId);
    Task<bool> ToggleStatusAsync(int id, int tenantId, bool enable);
    Task<Connection?> DuplicateAsync(int id, int tenantId, string newName);
}
