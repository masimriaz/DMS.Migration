using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Domain.Entities;

/// <summary>
/// Represents a discovered item (site, list, library, folder, file, content type, column)
/// </summary>
public class DiscoveryItem
{
    public Guid Id { get; set; }
    public Guid DiscoveryRunId { get; set; }
    public int TenantId { get; set; }

    public DiscoveryItemType ItemType { get; set; }

    // Hierarchy
    public Guid? ParentItemId { get; set; } // For nested items (subsite → site, list → site)
    public string Path { get; set; } = string.Empty; // Full URL path
    public int Level { get; set; } // Depth in hierarchy (0 = root site collection)

    // Basic properties
    public string Title { get; set; } = string.Empty;
    public string? InternalName { get; set; }
    public Guid? SharePointId { get; set; } // GUID from SharePoint

    // Counts and size
    public int ItemCount { get; set; }
    public int FolderCount { get; set; }
    public long SizeInBytes { get; set; }

    // Versioning
    public bool? VersioningEnabled { get; set; }
    public int? MajorVersionLimit { get; set; }
    public int? MinorVersionLimit { get; set; }
    public int? SampleVersionCount { get; set; } // Average versions per item (sample)

    // Checkout status
    public int? CheckedOutItemsCount { get; set; }

    // Permissions
    public bool? HasUniquePermissions { get; set; }
    public int? UniquePermissionCount { get; set; }

    // Content types and columns (for lists/libraries)
    public string? ContentTypesJson { get; set; } // JSONB array of content type names
    public string? ColumnsJson { get; set; } // JSONB array of column definitions

    // Template and customization flags
    public string? TemplateType { get; set; } // Document library, List, etc.
    public bool? HasCustomPages { get; set; }
    public bool? HasWebParts { get; set; }

    // Metadata as JSONB for extensibility
    public string MetadataJson { get; set; } = "{}";

    // Timestamps
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public DateTime DiscoveredAt { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public DiscoveryRun DiscoveryRun { get; set; } = null!;
    public DiscoveryItem? ParentItem { get; set; }
    public ICollection<DiscoveryItem> ChildItems { get; set; } = new List<DiscoveryItem>();
}
