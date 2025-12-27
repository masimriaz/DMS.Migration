using DMS.Migration.Application.Common.Abstractions;
using DMS.Migration.Application.Common.Models;
using DMS.Migration.Application.Connections.Interfaces;
using DMS.Migration.Application.Discovery.DTOs;
using DMS.Migration.Application.Discovery.Interfaces;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DMS.Migration.Application.Discovery.Commands;

public class CreateDiscoveryRunCommandHandler : ICommandHandler<CreateDiscoveryRunCommand, DiscoveryRunDto>
{
    private readonly IDiscoveryRepository _discoveryRepository;
    private readonly IConnectionRepository _connectionRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreateDiscoveryRunCommandHandler> _logger;

    public CreateDiscoveryRunCommandHandler(
        IDiscoveryRepository discoveryRepository,
        IConnectionRepository connectionRepository,
        ITenantContext tenantContext,
        ILogger<CreateDiscoveryRunCommandHandler> logger)
    {
        _discoveryRepository = discoveryRepository;
        _connectionRepository = connectionRepository;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<DiscoveryRunDto>> HandleAsync(
        CreateDiscoveryRunCommand command,
        CancellationToken cancellationToken = default)
    {
        var data = command.RunData;

        // Validate connection exists and belongs to tenant
        var connection = await _connectionRepository.GetByIdAsync(
            data.SourceConnectionId,
            _tenantContext.TenantId,
            cancellationToken);

        if (connection == null)
            return Result.Failure<DiscoveryRunDto>(
                Error.NotFound("Connection", data.SourceConnectionId));

        if (connection.Role != ConnectionRole.Source)
            return Result.Failure<DiscoveryRunDto>(
                Error.Validation("Only source connections can be used for discovery"));

        // Build configuration
        var configuration = new Dictionary<string, object>
        {
            ["ScanVersioning"] = data.ScanVersioning,
            ["ScanPermissions"] = data.ScanPermissions,
            ["ScanCheckedOutFiles"] = data.ScanCheckedOutFiles,
            ["ScanCustomPages"] = data.ScanCustomPages,
            ["MaxDepth"] = data.MaxDepth
        };

        // Create discovery run
        var run = new DiscoveryRun
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = data.RunName,
            SourceConnectionId = data.SourceConnectionId,
            ScopeUrl = data.ScopeUrl,
            Status = DiscoveryStatus.Queued,
            ConfigurationJson = System.Text.Json.JsonSerializer.Serialize(configuration),
            CorrelationId = _tenantContext.CorrelationId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.CurrentUser
        };

        var created = await _discoveryRepository.CreateRunAsync(run, cancellationToken);

        _logger.LogInformation(
            "Discovery run {RunId} '{RunName}' created for connection {ConnectionId} by {User}",
            created.Id, created.Name, data.SourceConnectionId, _tenantContext.CurrentUser);

        var dto = new DiscoveryRunDto
        {
            Id = (int)created.Id.GetHashCode(),
            Name = created.Name,
            SourceConnectionId = created.SourceConnectionId,
            SourceConnectionName = connection.Name,
            ScopeUrl = created.ScopeUrl,
            Status = created.Status,
            StartedAt = created.StartedAt,
            CompletedAt = created.CompletedAt,
            TotalItemsFound = created.TotalItemsScanned,
            TotalSizeBytes = 0,
            WarningCount = 0,
            ErrorMessage = created.ErrorMessage,
            CreatedAt = created.CreatedAt,
            CreatedBy = created.CreatedBy
        };

        return Result.Success(dto);
    }
}
