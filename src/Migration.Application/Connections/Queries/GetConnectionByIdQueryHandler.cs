using DMS.Migration.Application.Common.Abstractions;
using DMS.Migration.Application.Common.Models;
using DMS.Migration.Application.Connections.DTOs;
using DMS.Migration.Application.Connections.Interfaces;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Application.Connections.Queries;

public class GetConnectionByIdQueryHandler : IQueryHandler<GetConnectionByIdQuery, ConnectionDto>
{
    private readonly IConnectionRepository _repository;
    private readonly ITenantContext _tenantContext;

    public GetConnectionByIdQueryHandler(
        IConnectionRepository repository,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ConnectionDto>> HandleAsync(
        GetConnectionByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var connection = await _repository.GetByIdAsync(
            query.ConnectionId,
            _tenantContext.TenantId,
            cancellationToken);

        if (connection == null)
            return Result.Failure<ConnectionDto>(
                Error.NotFound("Connection", query.ConnectionId));

        var dto = MapToDto(connection);
        return Result.Success(dto);
    }

    private static ConnectionDto MapToDto(Connection c) => new()
    {
        Id = c.Id,
        TenantId = c.TenantId,
        Name = c.Name,
        Description = c.Description,
        Role = c.Role,
        Type = c.Type,
        Status = c.Status,
        EndpointUrl = c.EndpointUrl,
        AuthenticationMode = Enum.TryParse<AuthenticationMode>(c.AuthenticationMode, out var authMode) ? authMode : AuthenticationMode.OAuth,
        Username = c.Username,
        ThrottlingProfile = c.ThrottlingProfile,
        PreserveAuthorship = c.PreserveAuthorship,
        PreserveTimestamps = c.PreserveTimestamps,
        ReplaceIllegalCharacters = c.ReplaceIllegalCharacters,
        LastVerifiedAt = c.LastVerifiedAt,
        LastVerificationResult = !string.IsNullOrEmpty(c.LastVerificationResult) && Enum.TryParse<VerificationResult>(c.LastVerificationResult, out var verifyResult) ? verifyResult : (VerificationResult?)null,
        LastVerificationDiagnostics = c.LastVerificationDiagnostics,
        CreatedAt = c.CreatedAt,
        CreatedBy = c.CreatedBy
    };
}
