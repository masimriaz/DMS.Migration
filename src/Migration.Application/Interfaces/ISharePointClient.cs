using DMS.Migration.Domain.Entities;

namespace DMS.Migration.Application.Interfaces;

/// <summary>
/// Abstraction for SharePoint operations - supports both SharePoint Online and On-Premises
/// </summary>
public interface ISharePointClient
{
    /// <summary>
    /// Verify connection to SharePoint and test permissions
    /// </summary>
    Task<(bool Success, string Message, Dictionary<string, object> Diagnostics)> VerifyConnectionAsync(
        Connection connection,
        string decryptedSecret);

    /// <summary>
    /// Discover all libraries and their metadata
    /// </summary>
    Task<LibraryInventory> DiscoverLibrariesAsync(
        Connection connection,
        string decryptedSecret);

    /// <summary>
    /// Scan a specific library and retrieve all items
    /// </summary>
    Task<List<DocumentItem>> ScanLibraryAsync(
        Connection connection,
        string decryptedSecret,
        string libraryTitle,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a SharePoint library inventory result
/// </summary>
public class LibraryInventory
{
    public string SiteUrl { get; set; } = string.Empty;
    public string SiteTitle { get; set; } = string.Empty;
    public List<LibraryInfo> Libraries { get; set; } = new();
    public int TotalItemCount { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Information about a SharePoint library
/// </summary>
public class LibraryInfo
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string BaseType { get; set; } = string.Empty;
    public bool Hidden { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastModified { get; set; }
}

/// <summary>
/// Represents a document or folder in SharePoint
/// </summary>
public class DocumentItem
{
    public string Name { get; set; } = string.Empty;
    public string RelativeUrl { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty; // File or Folder
    public long? FileSize { get; set; }
    public string? FileExtension { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string ModifiedBy { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
