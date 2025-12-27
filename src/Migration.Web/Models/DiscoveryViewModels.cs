using System.ComponentModel.DataAnnotations;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Web.Models;

public class DiscoveryIndexViewModel
{
    public List<DiscoveryRun> Runs { get; set; } = new();
    public int CurrentPage { get; set; }
    public DiscoveryStatus? FilterStatus { get; set; }
}

public class NewDiscoveryViewModel
{
    [Required(ErrorMessage = "Please enter a name for this discovery run")]
    [StringLength(200, MinimumLength = 3)]
    [Display(Name = "Run Name")]
    public string RunName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a source connection")]
    [Display(Name = "Source Connection")]
    public int SourceConnectionId { get; set; }

    [Required(ErrorMessage = "Please specify the scope URL")]
    [Url(ErrorMessage = "Please enter a valid URL")]
    [Display(Name = "Scope URL")]
    public string ScopeUrl { get; set; } = string.Empty;

    [Display(Name = "Scan Versioning Settings")]
    public bool ScanVersioning { get; set; } = true;

    [Display(Name = "Scan Permissions")]
    public bool ScanPermissions { get; set; } = true;

    [Display(Name = "Scan Checked-Out Files")]
    public bool ScanCheckedOutFiles { get; set; } = true;

    [Display(Name = "Scan Custom Pages & Web Parts")]
    public bool ScanCustomPages { get; set; } = false;

    [Range(1, 10)]
    [Display(Name = "Max Scan Depth")]
    public int MaxDepth { get; set; } = 5;

    public List<Connection> AvailableConnections { get; set; } = new();
}

public class DiscoveryDetailsViewModel
{
    public DiscoveryRun Run { get; set; } = null!;
    public Dictionary<string, long> SummaryMetrics { get; set; } = new();
    public int TotalWarnings { get; set; }
    public int CriticalWarnings { get; set; }
}

public class DiscoveryItemsViewModel
{
    public DiscoveryRun Run { get; set; } = null!;
    public List<DiscoveryItem> Items { get; set; } = new();
    public DiscoveryItemType? FilterType { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
}

public class DiscoveryWarningsViewModel
{
    public DiscoveryRun Run { get; set; } = null!;
    public List<DiscoveryWarning> Warnings { get; set; } = new();
    public DiscoverySeverity? FilterSeverity { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalWarnings { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalWarnings / (double)PageSize);
}
