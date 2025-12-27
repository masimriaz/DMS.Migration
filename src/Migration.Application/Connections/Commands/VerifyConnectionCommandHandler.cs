using DMS.Migration.Application.Common.Abstractions;
using DMS.Migration.Application.Common.Models;
using DMS.Migration.Application.Connections.DTOs;
using DMS.Migration.Application.Connections.Interfaces;
using DMS.Migration.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DMS.Migration.Application.Connections.Commands;

public class VerifyConnectionCommandHandler : ICommandHandler<VerifyConnectionCommand, VerificationResultDto>
{
    private readonly IConnectionRepository _repository;
    private readonly IConnectionVerifierFactory _verifierFactory;
    private readonly IEncryptionService _encryptionService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<VerifyConnectionCommandHandler> _logger;

    public VerifyConnectionCommandHandler(
        IConnectionRepository repository,
        IConnectionVerifierFactory verifierFactory,
        IEncryptionService encryptionService,
        ITenantContext tenantContext,
        ILogger<VerifyConnectionCommandHandler> logger)
    {
        _repository = repository;
        _verifierFactory = verifierFactory;
        _encryptionService = encryptionService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<VerificationResultDto>> HandleAsync(
        VerifyConnectionCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get connection with tenant check
        var connection = await _repository.GetByIdAsync(
            command.ConnectionId,
            _tenantContext.TenantId,
            cancellationToken);

        if (connection == null)
            return Result.Failure<VerificationResultDto>(
                Error.NotFound("Connection", command.ConnectionId));

        // Decrypt password if exists
        string? decryptedPassword = null;
        if (connection.Secret != null)
        {
            decryptedPassword = _encryptionService.Decrypt(connection.Secret.EncryptedSecret);
        }

        // Get appropriate verifier and execute
        var verifier = _verifierFactory.GetVerifier(connection);
        var result = await verifier.VerifyAsync(connection, decryptedPassword, cancellationToken);

        // Update connection status
        connection.LastVerifiedAt = result.VerifiedAt;
        connection.LastVerificationResult = result.IsSuccessful
            ? VerificationResult.Success.ToString()
            : VerificationResult.Failed.ToString();
        connection.LastVerificationDiagnostics = result.Message;
        connection.Status = result.IsSuccessful
            ? ConnectionStatus.Active
            : ConnectionStatus.Error;
        connection.UpdatedBy = _tenantContext.CurrentUser;

        await _repository.UpdateAsync(connection, cancellationToken);

        _logger.LogInformation(
            "Connection {ConnectionId} verification {Result}: {Message}",
            command.ConnectionId,
            result.IsSuccessful ? "succeeded" : "failed",
            result.Message);

        return Result.Success(result);
    }
}
