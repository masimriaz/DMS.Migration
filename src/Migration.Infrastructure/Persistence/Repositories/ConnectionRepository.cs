using Microsoft.EntityFrameworkCore;
using DMS.Migration.Application.Connections.Interfaces;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;
using DMS.Migration.Infrastructure.Data;

namespace DMS.Migration.Infrastructure.Persistence.Repositories;

public class ConnectionRepository : IConnectionRepository
{
    private readonly AppDbContext _context;

    public ConnectionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Connection?> GetByIdAsync(int id, int tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Connections
            .Include(c => c.Secret)
            .Include(c => c.VerificationRuns.OrderByDescending(v => v.StartedAt).Take(5))
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId && !c.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Connection>> GetAllAsync(
        int tenantId,
        ConnectionRole? role = null,
        ConnectionType? type = null,
        ConnectionStatus? status = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Connections
            .Include(c => c.Secret)
            .Where(c => c.TenantId == tenantId && !c.IsDeleted);

        if (role.HasValue)
            query = query.Where(c => c.Role == role.Value);

        if (type.HasValue)
            query = query.Where(c => c.Type == type.Value);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.Description != null && c.Description.ToLower().Contains(term)) ||
                c.EndpointUrl.ToLower().Contains(term));
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Connection> CreateAsync(Connection connection, CancellationToken cancellationToken = default)
    {
        _context.Connections.Add(connection);
        await _context.SaveChangesAsync(cancellationToken);
        return connection;
    }

    public async Task UpdateAsync(Connection connection, CancellationToken cancellationToken = default)
    {
        connection.UpdatedAt = DateTime.UtcNow;
        _context.Connections.Update(connection);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, int tenantId, CancellationToken cancellationToken = default)
    {
        var connection = await GetByIdAsync(id, tenantId, cancellationToken);
        if (connection != null)
        {
            connection.IsDeleted = true;
            connection.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(int id, int tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Connections
            .AnyAsync(c => c.Id == id && c.TenantId == tenantId && !c.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Connection>> GetByRoleAsync(
        int tenantId,
        ConnectionRole role,
        CancellationToken cancellationToken = default)
    {
        return await _context.Connections
            .Where(c => c.TenantId == tenantId && c.Role == role && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
