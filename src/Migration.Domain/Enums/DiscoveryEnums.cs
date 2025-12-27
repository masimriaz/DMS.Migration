namespace DMS.Migration.Domain.Enums;

/// <summary>
/// Status of a discovery run
/// </summary>
public enum DiscoveryStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Paused = 5
}

/// <summary>
/// Type of discovered item
/// </summary>
public enum DiscoveryItemType
{
    SiteCollection = 1,
    Site = 2,
    Web = 3,
    List = 4,
    Library = 5,
    Folder = 6,
    File = 7,
    ContentType = 8,
    Column = 9,
    View = 10,
    WebPart = 11
}

/// <summary>
/// Severity level for discovery warnings
/// </summary>
public enum DiscoverySeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3,
    Blocker = 4 // Migration-blocking issue
}

/// <summary>
/// Export format for discovery results
/// </summary>
public enum DiscoveryExportFormat
{
    Json = 1,
    Csv = 2,
    Excel = 3,
    Pdf = 4
}

/// <summary>
/// Type of export report
/// </summary>
public enum DiscoveryExportType
{
    Summary = 1,
    DetailedInventory = 2,
    WarningsOnly = 3,
    MetricsOnly = 4,
    SiteHierarchy = 5,
    ContentTypeMapping = 6,
    PermissionSummary = 7
}
