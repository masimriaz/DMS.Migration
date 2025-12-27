using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Application.Connections.Interfaces;

/// <summary>
/// Repository for Connection entity with tenant-aware operations
/// </summary>
public interface IConnectionRepository
{
    Task<Connection?> GetByIdAsync(int id, int tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Connection>> GetAllAsync(
        int tenantId,
        ConnectionRole? role = null,
        ConnectionType? type = null,
        ConnectionStatus? status = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<Connection> CreateAsync(Connection connection, CancellationToken cancellationToken = default);

    Task UpdateAsync(Connection connection, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, int tenantId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, int tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Connection>> GetByRoleAsync(int tenantId, ConnectionRole role, CancellationToken cancellationToken = default);
}
