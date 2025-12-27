using DMS.Migration.Application.Common.Abstractions;
using DMS.Migration.Application.Discovery.DTOs;

namespace DMS.Migration.Application.Discovery.Commands;

/// <summary>
/// Command to create and start a new discovery run
/// </summary>
public record CreateDiscoveryRunCommand : ICommand<DiscoveryRunDto>
{
    public CreateDiscoveryRunDto RunData { get; init; } = null!;
}
