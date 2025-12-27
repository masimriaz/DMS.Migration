using DMS.Migration.Domain.Entities;

namespace DMS.Migration.Infrastructure.SharePoint.Abstractions;

/// <summary>
/// Authentication service for SharePoint connections
/// </summary>
public interface ISharePointAuthService
{
    Task<string> GetAccessTokenAsync(Connection connection, string? password, CancellationToken cancellationToken = default);
}

/// <summary>
/// Client for SharePoint discovery operations
/// </summary>
public interface ISharePointDiscoveryClient
{
    IAsyncEnumerable<DiscoveryItem> DiscoverAsync(
        string accessToken,
        string siteUrl,
        string scopeUrl,
        Dictionary<string, object> configuration,
        int tenantId,
        int runId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Client for SharePoint migration operations
/// </summary>
public interface ISharePointMigrationClient
{
    Task<bool> UploadFileAsync(
        string accessToken,
        string targetUrl,
        Stream fileContent,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<bool> CreateFolderAsync(
        string accessToken,
        string targetUrl,
        string folderName,
        CancellationToken cancellationToken = default);
}
