using DMS.Migration.Application.Common.Abstractions;
using DMS.Migration.Application.Connections.DTOs;

namespace DMS.Migration.Application.Connections.Commands;

/// <summary>
/// Command to verify a connection's credentials and connectivity
/// </summary>
public record VerifyConnectionCommand : ICommand<VerificationResultDto>
{
    public int ConnectionId { get; init; }
}
