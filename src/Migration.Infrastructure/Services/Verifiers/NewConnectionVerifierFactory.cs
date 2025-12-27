using DMS.Migration.Application.Connections.DTOs;
using DMS.Migration.Application.Connections.Interfaces;
using DMS.Migration.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace DMS.Migration.Infrastructure.Services.Verifiers;

/// <summary>
/// Factory that implements the new IConnectionVerifierFactory from Connections module
/// </summary>
public class NewConnectionVerifierFactory : IConnectionVerifierFactory
{
    private readonly IServiceProvider _serviceProvider;

    public NewConnectionVerifierFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IConnectionVerifier GetVerifier(Connection connection)
    {
        // For now, return a simple verifier that wraps the old implementation
        return new ConnectionVerifierAdapter(_serviceProvider, connection);
    }
}

/// <summary>
/// Adapter that implements new IConnectionVerifier using old verifier infrastructure
/// </summary>
internal class ConnectionVerifierAdapter : IConnectionVerifier
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Connection _connection;

    public ConnectionVerifierAdapter(IServiceProvider serviceProvider, Connection connection)
    {
        _serviceProvider = serviceProvider;
        _connection = connection;
    }

    public async Task<VerificationResultDto> VerifyAsync(
        Connection connection,
        string? decryptedPassword,
        CancellationToken cancellationToken = default)
    {
        // Get the old-style factory
        var oldFactory = _serviceProvider.GetRequiredService<Application.Interfaces.IConnectionVerifierFactory>();
        var oldVerifier = oldFactory.GetVerifier(connection.Type);

        // Call old verifier (which returns ConnectionVerificationRun)
        var run = await oldVerifier.VerifyAsync(connection, "System");

        // Map to new DTO
        return new VerificationResultDto
        {
            IsSuccessful = run.Result == "Success",
            Message = run.Diagnostics ?? run.ErrorMessage ?? string.Empty,
            VerifiedAt = run.StartedAt
        };
    }
}
