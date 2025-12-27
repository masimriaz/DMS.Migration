using DMS.Migration.Application.Common.Abstractions;
using DMS.Migration.Application.Common.Models;
using DMS.Migration.Application.Connections.DTOs;
using DMS.Migration.Application.Connections.Interfaces;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DMS.Migration.Application.Connections.Commands;

public class CreateConnectionCommandHandler : ICommandHandler<CreateConnectionCommand, ConnectionDto>
{
    private readonly IConnectionRepository _repository;
    private readonly IEncryptionService _encryptionService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreateConnectionCommandHandler> _logger;

    public CreateConnectionCommandHandler(
        IConnectionRepository repository,
        IEncryptionService encryptionService,
        ITenantContext tenantContext,
        ILogger<CreateConnectionCommandHandler> logger)
    {
        _repository = repository;
        _encryptionService = encryptionService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<ConnectionDto>> HandleAsync(
        CreateConnectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var data = command.ConnectionData;

        // Create entity
        var connection = new Connection
        {
            TenantId = _tenantContext.TenantId,
            Name = data.Name,
            Description = data.Description,
            Role = data.Role,
            Type = data.Type,
            Status = ConnectionStatus.Draft,
            EndpointUrl = data.EndpointUrl,
            AuthenticationMode = data.AuthenticationMode.ToString(),
            Username = data.Username,
            ThrottlingProfile = data.ThrottlingProfile,
            PreserveAuthorship = data.PreserveAuthorship,
            PreserveTimestamps = data.PreserveTimestamps,
            ReplaceIllegalCharacters = data.ReplaceIllegalCharacters,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.CurrentUser,
            UpdatedBy = _tenantContext.CurrentUser
        };

        // Create encrypted secret if password provided
        if (!string.IsNullOrWhiteSpace(data.Password))
        {
            connection.Secret = new ConnectionSecret
            {
                EncryptedSecret = _encryptionService.Encrypt(data.Password),
                CreatedAt = DateTime.UtcNow
            };
        }

        var created = await _repository.CreateAsync(connection, cancellationToken);

        _logger.LogInformation(
            "Connection {ConnectionId} '{ConnectionName}' created by {User} in tenant {TenantId}",
            created.Id, created.Name, _tenantContext.CurrentUser, _tenantContext.TenantId);

        var dto = MapToDto(created);
        return Result.Success(dto);
    }

    private static ConnectionDto MapToDto(Connection connection) => new()
    {
        Id = connection.Id,
        TenantId = connection.TenantId,
        Name = connection.Name,
        Description = connection.Description,
        Role = connection.Role,
        Type = connection.Type,
        Status = connection.Status,
        EndpointUrl = connection.EndpointUrl,
        AuthenticationMode = Enum.TryParse<AuthenticationMode>(connection.AuthenticationMode, out var authMode) ? authMode : AuthenticationMode.OAuth,
        Username = connection.Username,
        ThrottlingProfile = connection.ThrottlingProfile,
        PreserveAuthorship = connection.PreserveAuthorship,
        PreserveTimestamps = connection.PreserveTimestamps,
        ReplaceIllegalCharacters = connection.ReplaceIllegalCharacters,
        LastVerifiedAt = connection.LastVerifiedAt,
        LastVerificationResult = !string.IsNullOrEmpty(connection.LastVerificationResult) && Enum.TryParse<VerificationResult>(connection.LastVerificationResult, out var verifyResult) ? verifyResult : (VerificationResult?)null,
        LastVerificationDiagnostics = connection.LastVerificationDiagnostics,
        CreatedAt = connection.CreatedAt,
        CreatedBy = connection.CreatedBy
    };
}
