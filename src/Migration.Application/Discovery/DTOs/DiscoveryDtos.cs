using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Application.Discovery.DTOs;

public record DiscoveryRunDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int SourceConnectionId { get; init; }
    public string SourceConnectionName { get; init; } = string.Empty;
    public string ScopeUrl { get; init; } = string.Empty;
    public DiscoveryStatus Status { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public long TotalItemsFound { get; init; }
    public long TotalSizeBytes { get; init; }
    public int WarningCount { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}

public record CreateDiscoveryRunDto
{
    public string RunName { get; init; } = string.Empty;
    public int SourceConnectionId { get; init; }
    public string ScopeUrl { get; init; } = string.Empty;
    public bool ScanVersioning { get; init; }
    public bool ScanPermissions { get; init; }
    public bool ScanCheckedOutFiles { get; init; }
    public bool ScanCustomPages { get; init; }
    public int MaxDepth { get; init; } = 10;
}

public record DiscoveryItemDto
{
    public string Url { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTime? ModifiedDate { get; init; }
    public string? ModifiedBy { get; init; }
}
