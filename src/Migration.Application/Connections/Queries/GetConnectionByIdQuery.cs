using DMS.Migration.Application.Common.Abstractions;
using DMS.Migration.Application.Connections.DTOs;

namespace DMS.Migration.Application.Connections.Queries;

/// <summary>
/// Query to get a connection by ID
/// </summary>
public record GetConnectionByIdQuery : IQuery<ConnectionDto>
{
    public int ConnectionId { get; init; }
}
