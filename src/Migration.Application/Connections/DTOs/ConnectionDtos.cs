using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Application.Connections.DTOs;

public record ConnectionDto
{
    public int Id { get; init; }
    public int TenantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public ConnectionRole Role { get; init; }
    public ConnectionType Type { get; init; }
    public ConnectionStatus Status { get; init; }
    public string EndpointUrl { get; init; } = string.Empty;
    public AuthenticationMode AuthenticationMode { get; init; }
    public string? Username { get; init; }
    public ThrottlingProfile ThrottlingProfile { get; init; }
    public bool PreserveAuthorship { get; init; }
    public bool PreserveTimestamps { get; init; }
    public bool ReplaceIllegalCharacters { get; init; }
    public DateTime? LastVerifiedAt { get; init; }
    public VerificationResult? LastVerificationResult { get; init; }
    public string? LastVerificationDiagnostics { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}

public record CreateConnectionDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public ConnectionRole Role { get; init; }
    public ConnectionType Type { get; init; }
    public string EndpointUrl { get; init; } = string.Empty;
    public AuthenticationMode AuthenticationMode { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public ThrottlingProfile ThrottlingProfile { get; init; }
    public bool PreserveAuthorship { get; init; }
    public bool PreserveTimestamps { get; init; }
    public bool ReplaceIllegalCharacters { get; init; }
}

public record UpdateConnectionDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string EndpointUrl { get; init; } = string.Empty;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public ThrottlingProfile ThrottlingProfile { get; init; }
    public bool PreserveAuthorship { get; init; }
    public bool PreserveTimestamps { get; init; }
    public bool ReplaceIllegalCharacters { get; init; }
}

public record VerificationResultDto
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, object>? Diagnostics { get; init; }
    public DateTime VerifiedAt { get; init; }
}
