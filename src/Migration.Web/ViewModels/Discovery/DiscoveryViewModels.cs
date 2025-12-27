using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Web.ViewModels.Discovery;

public class DiscoveryIndexViewModel
{
    public IEnumerable<DiscoveryRunListItem> Runs { get; set; } = Enumerable.Empty<DiscoveryRunListItem>();
    public int CurrentPage { get; set; }
    public DiscoveryStatus? FilterStatus { get; set; }
}

public class DiscoveryRunListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SourceConnectionName { get; set; } = string.Empty;
    public DiscoveryStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long TotalItemsFound { get; set; }
    public long TotalSizeBytes { get; set; }
    public int WarningCount { get; set; }
}

public class CreateDiscoveryFormViewModel
{
    public string RunName { get; set; } = string.Empty;
    public int SourceConnectionId { get; set; }
    public string ScopeUrl { get; set; } = string.Empty;
    public bool ScanVersioning { get; set; }
    public bool ScanPermissions { get; set; }
    public bool ScanCheckedOutFiles { get; set; }
    public bool ScanCustomPages { get; set; }
    public int MaxDepth { get; set; } = 10;
}
