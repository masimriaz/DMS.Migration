using DMS.Migration.Application.Common.Abstractions;
using DMS.Migration.Application.Connections.DTOs;

namespace DMS.Migration.Application.Connections.Commands;

/// <summary>
/// Command to create a new connection
/// </summary>
public record CreateConnectionCommand : ICommand<ConnectionDto>
{
    public CreateConnectionDto ConnectionData { get; init; } = null!;
}
