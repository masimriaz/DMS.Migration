using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DMS.Migration.Application.Interfaces;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;
using DMS.Migration.Infrastructure.Data;

namespace DMS.Migration.Infrastructure.Services;

public class ConnectionService : IConnectionService
{
    private readonly AppDbContext _context;
    private readonly IDataProtector _protector;
    private readonly IAuditService _auditService;
    private readonly ILogger<ConnectionService> _logger;

    public ConnectionService(
        AppDbContext context,
        IDataProtectionProvider protectionProvider,
        IAuditService auditService,
        ILogger<ConnectionService> logger)
    {
        _context = context;
        _protector = protectionProvider.CreateProtector("ConnectionSecrets");
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IEnumerable<Connection>> GetAllAsync(
        int tenantId,
        ConnectionRole? role = null,
        ConnectionType? type = null,
        ConnectionStatus? status = null,
        string? searchTerm = null)
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
            .ToListAsync();
    }

    public async Task<Connection?> GetByIdAsync(int id, int tenantId)
    {
        return await _context.Connections
            .Include(c => c.Secret)
            .Include(c => c.VerificationRuns.OrderByDescending(v => v.StartedAt).Take(5))
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId && !c.IsDeleted);
    }

    public async Task<Connection> CreateAsync(Connection connection, string? password = null)
    {
        connection.CreatedAt = DateTime.UtcNow;
        connection.Status = ConnectionStatus.Draft;

        _context.Connections.Add(connection);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(password))
        {
            var secret = new ConnectionSecret
            {
                ConnectionId = connection.Id,
                EncryptedSecret = _protector.Protect(password),
                CreatedAt = DateTime.UtcNow
            };
            _context.ConnectionSecrets.Add(secret);
            await _context.SaveChangesAsync();
        }

        await _auditService.LogAsync(
            connection.TenantId,
            "Connection",
            connection.Id,
            "Create",
            connection.CreatedBy,
            connection.CreatedBy,
            null,
            $"Created {connection.Type} connection '{connection.Name}'");

        _logger.LogInformation("Created connection {ConnectionId} for tenant {TenantId}", connection.Id, connection.TenantId);

        return connection;
    }

    public async Task<Connection> UpdateAsync(Connection connection, string? password = null)
    {
        var existing = await _context.Connections
            .Include(c => c.Secret)
            .FirstOrDefaultAsync(c => c.Id == connection.Id && c.TenantId == connection.TenantId);

        if (existing == null)
            throw new InvalidOperationException("Connection not found");

        existing.Name = connection.Name;
        existing.Description = connection.Description;
        existing.Role = connection.Role;
        existing.Type = connection.Type;
        existing.EndpointUrl = connection.EndpointUrl;
        existing.AuthenticationMode = connection.AuthenticationMode;
        existing.Username = connection.Username;
        existing.ThrottlingProfile = connection.ThrottlingProfile;
        existing.PreserveAuthorship = connection.PreserveAuthorship;
        existing.PreserveTimestamps = connection.PreserveTimestamps;
        existing.ReplaceIllegalCharacters = connection.ReplaceIllegalCharacters;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = connection.UpdatedBy;

        if (!string.IsNullOrWhiteSpace(password))
        {
            if (existing.Secret != null)
            {
                existing.Secret.EncryptedSecret = _protector.Protect(password);
                existing.Secret.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                existing.Secret = new ConnectionSecret
                {
                    ConnectionId = existing.Id,
                    EncryptedSecret = _protector.Protect(password),
                    CreatedAt = DateTime.UtcNow
                };
            }
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            existing.TenantId,
            "Connection",
            existing.Id,
            "Update",
            existing.UpdatedBy ?? "System",
            existing.UpdatedBy ?? "System",
            null,
            $"Updated connection '{existing.Name}'");

        _logger.LogInformation("Updated connection {ConnectionId} for tenant {TenantId}", existing.Id, existing.TenantId);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, int tenantId)
    {
        var connection = await _context.Connections
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId && !c.IsDeleted);

        if (connection == null)
            return false;

        connection.IsDeleted = true;
        connection.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            tenantId,
            "Connection",
            id,
            "Delete",
            "System",
            "System",
            null,
            $"Deleted connection '{connection.Name}'");

        _logger.LogInformation("Deleted connection {ConnectionId} for tenant {TenantId}", id, tenantId);

        return true;
    }

    public async Task<bool> ToggleStatusAsync(int id, int tenantId, bool enable)
    {
        var connection = await _context.Connections
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId && !c.IsDeleted);

        if (connection == null)
            return false;

        connection.Status = enable ? ConnectionStatus.Verified : ConnectionStatus.Disabled;
        connection.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            tenantId,
            "Connection",
            id,
            enable ? "Enable" : "Disable",
            "System",
            "System",
            null,
            $"{(enable ? "Enabled" : "Disabled")} connection '{connection.Name}'");

        return true;
    }

    public async Task<Connection?> DuplicateAsync(int id, int tenantId, string newName)
    {
        var source = await GetByIdAsync(id, tenantId);
        if (source == null)
            return null;

        var duplicate = new Connection
        {
            TenantId = source.TenantId,
            Name = newName,
            Description = source.Description,
            Role = source.Role,
            Type = source.Type,
            Status = ConnectionStatus.Draft,
            EndpointUrl = source.EndpointUrl,
            AuthenticationMode = source.AuthenticationMode,
            Username = source.Username,
            ThrottlingProfile = source.ThrottlingProfile,
            PreserveAuthorship = source.PreserveAuthorship,
            PreserveTimestamps = source.PreserveTimestamps,
            ReplaceIllegalCharacters = source.ReplaceIllegalCharacters,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        _context.Connections.Add(duplicate);
        await _context.SaveChangesAsync();

        if (source.Secret != null)
        {
            var decryptedSecret = _protector.Unprotect(source.Secret.EncryptedSecret);
            var newSecret = new ConnectionSecret
            {
                ConnectionId = duplicate.Id,
                EncryptedSecret = _protector.Protect(decryptedSecret),
                CreatedAt = DateTime.UtcNow
            };
            _context.ConnectionSecrets.Add(newSecret);
            await _context.SaveChangesAsync();
        }

        await _auditService.LogAsync(
            tenantId,
            "Connection",
            duplicate.Id,
            "Duplicate",
            "System",
            "System",
            null,
            $"Duplicated from connection '{source.Name}'");

        return duplicate;
    }

    public string? DecryptPassword(ConnectionSecret secret)
    {
        try
        {
            return _protector.Unprotect(secret.EncryptedSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt password for connection secret {SecretId}", secret.Id);
            return null;
        }
    }
}
