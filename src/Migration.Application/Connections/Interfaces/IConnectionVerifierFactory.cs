using DMS.Migration.Application.Connections.DTOs;
using DMS.Migration.Domain.Entities;

namespace DMS.Migration.Application.Connections.Interfaces;

/// <summary>
/// Factory for creating connection verifiers based on connection type
/// </summary>
public interface IConnectionVerifierFactory
{
    IConnectionVerifier GetVerifier(Connection connection);
}

/// <summary>
/// Service for verifying connection credentials and connectivity
/// </summary>
public interface IConnectionVerifier
{
    Task<VerificationResultDto> VerifyAsync(Connection connection, string? decryptedPassword, CancellationToken cancellationToken = default);
}
