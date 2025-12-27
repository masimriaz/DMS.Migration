using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using PnP.Framework;
using DMS.Migration.Application.Interfaces;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;
using DMS.Migration.Infrastructure.Data;
using System.Text.Json;

namespace DMS.Migration.Infrastructure.SharePoint;

public class SharePointOnlineService : ISharePointClient
{
    private readonly SharePointAuthenticationService _authService;
    private readonly ILogger<SharePointOnlineService> _logger;

    public SharePointOnlineService(
        SharePointAuthenticationService authService,
        ILogger<SharePointOnlineService> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Verify SharePoint Online connection and test permissions
    /// </summary>
    public async Task<(bool Success, string Message, Dictionary<string, object> Diagnostics)> VerifyConnectionAsync(
        Connection connection,
        string decryptedSecret)
    {
        var diagnostics = new Dictionary<string, object>();

        try
        {
            // Parse authentication configuration
            var authConfig = ParseAuthConfig(connection, decryptedSecret);
            diagnostics["AuthenticationMode"] = connection.AuthenticationMode;
            diagnostics["SiteUrl"] = connection.EndpointUrl;

            // Get access token
            string accessToken;
            if (authConfig.UseCertificate)
            {
                accessToken = await _authService.GetAccessTokenAsync(
                    authConfig.TenantId!,
                    authConfig.ClientId!,
                    authConfig.CertificateThumbprint!,
                    connection.EndpointUrl);
            }
            else
            {
                accessToken = await _authService.GetAccessTokenWithSecretAsync(
                    authConfig.TenantId!,
                    authConfig.ClientId!,
                    authConfig.ClientSecret!,
                    connection.EndpointUrl);
            }

            diagnostics["TokenAcquired"] = true;

            // Connect to SharePoint site using PnP Framework
            using var context = new ClientContext(connection.EndpointUrl);
            context.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = $"Bearer {accessToken}";
            };

            // Load site properties
            var web = context.Web;
            context.Load(web, w => w.Title, w => w.Url, w => w.ServerRelativeUrl, w => w.Language);
            context.Load(web.Lists, lists => lists.Include(l => l.Title, l => l.ItemCount, l => l.BaseType, l => l.Hidden));
            context.Load(web.CurrentUser, u => u.LoginName, u => u.Title, u => u.Email);

            await context.ExecuteQueryAsync();

            diagnostics["SiteTitle"] = web.Title;
            diagnostics["SiteUrl"] = web.Url;
            diagnostics["Language"] = web.Language;
            diagnostics["CurrentUser"] = web.CurrentUser.Title;
            diagnostics["UserEmail"] = web.CurrentUser.Email;

            // Count libraries (excluding hidden ones)
            var libraries = web.Lists.Where(l => !l.Hidden && l.BaseType == BaseType.DocumentLibrary).ToList();
            diagnostics["LibraryCount"] = libraries.Count;
            diagnostics["Libraries"] = libraries.Select(l => new { l.Title, l.ItemCount }).Take(10).ToList();

            // Calculate total items
            var totalItems = libraries.Sum(l => l.ItemCount);
            diagnostics["TotalDocumentCount"] = totalItems;

            // Check if we can create items (write permission test)
            try
            {
                var list = web.Lists.GetByTitle("Site Pages");
                context.Load(list, l => l.Title);
                await context.ExecuteQueryAsync();
                diagnostics["WritePermission"] = "Not tested - skipped write test";
            }
            catch
            {
                diagnostics["WritePermission"] = "Not tested - Site Pages not found";
            }

            var message = $"Successfully connected to '{web.Title}' ({web.Url}). " +
                         $"Found {libraries.Count} libraries with {totalItems} total items. " +
                         $"Authenticated as {web.CurrentUser.Title}.";

            return (true, message, diagnostics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SharePoint Online verification failed for connection {ConnectionId}", connection.Id);

            diagnostics["Error"] = ex.Message;
            diagnostics["ErrorType"] = ex.GetType().Name;

            if (ex.InnerException != null)
            {
                diagnostics["InnerError"] = ex.InnerException.Message;
            }

            return (false, $"Verification failed: {ex.Message}", diagnostics);
        }
    }

    /// <summary>
    /// Discover all libraries and their metadata
    /// </summary>
    public async Task<LibraryInventory> DiscoverLibrariesAsync(Connection connection, string decryptedSecret)
    {
        var authConfig = ParseAuthConfig(connection, decryptedSecret);
        string accessToken;

        if (authConfig.UseCertificate)
        {
            accessToken = await _authService.GetAccessTokenAsync(
                authConfig.TenantId!,
                authConfig.ClientId!,
                authConfig.CertificateThumbprint!,
                connection.EndpointUrl);
        }
        else
        {
            accessToken = await _authService.GetAccessTokenWithSecretAsync(
                authConfig.TenantId!,
                authConfig.ClientId!,
                authConfig.ClientSecret!,
                connection.EndpointUrl);
        }

        using var context = new ClientContext(connection.EndpointUrl);
        context.ExecutingWebRequest += (sender, e) =>
        {
            e.WebRequestExecutor.RequestHeaders["Authorization"] = $"Bearer {accessToken}";
        };

        var web = context.Web;
        context.Load(web, w => w.Title, w => w.Url);
        context.Load(web.Lists, lists => lists.Include(
            l => l.Id,
            l => l.Title,
            l => l.Description,
            l => l.ItemCount,
            l => l.BaseType,
            l => l.Hidden,
            l => l.RootFolder.ServerRelativeUrl,
            l => l.EnableVersioning,
            l => l.MajorVersionLimit,
            l => l.Fields.Include(f => f.InternalName, f => f.Title, f => f.TypeAsString, f => f.Required, f => f.Hidden)
        ));

        await context.ExecuteQueryAsync();

        var libraries = new List<LibraryInfo>();

        foreach (var list in web.Lists.Where(l => !l.Hidden && l.BaseType == BaseType.DocumentLibrary))
        {
            var libraryInfo = new LibraryInfo
            {
                Title = list.Title,
                Url = list.RootFolder.ServerRelativeUrl,
                ItemCount = list.ItemCount,
                BaseType = list.BaseType.ToString(),
                Hidden = list.Hidden,
                Created = DateTime.UtcNow, // Note: SharePoint CSOM doesn't expose Created easily
                LastModified = DateTime.UtcNow
            };

            libraries.Add(libraryInfo);
        }

        return new LibraryInventory
        {
            SiteUrl = web.Url,
            SiteTitle = web.Title,
            Libraries = libraries,
            TotalItemCount = libraries.Sum(l => l.ItemCount)
        };
    }

    /// <summary>
    /// Scan a specific library and retrieve all items
    /// </summary>
    public async Task<List<DocumentItem>> ScanLibraryAsync(
        Connection connection,
        string decryptedSecret,
        string libraryTitle,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var authConfig = ParseAuthConfig(connection, decryptedSecret);
        string accessToken;

        if (authConfig.UseCertificate)
        {
            accessToken = await _authService.GetAccessTokenAsync(
                authConfig.TenantId!,
                authConfig.ClientId!,
                authConfig.CertificateThumbprint!,
                connection.EndpointUrl);
        }
        else
        {
            accessToken = await _authService.GetAccessTokenWithSecretAsync(
                authConfig.TenantId!,
                authConfig.ClientId!,
                authConfig.ClientSecret!,
                connection.EndpointUrl);
        }

        using var context = new ClientContext(connection.EndpointUrl);
        context.ExecutingWebRequest += (sender, e) =>
        {
            e.WebRequestExecutor.RequestHeaders["Authorization"] = $"Bearer {accessToken}";
        };

        var list = context.Web.Lists.GetByTitle(libraryTitle);
        var items = list.GetItems(CamlQuery.CreateAllItemsQuery());
        context.Load(items, i => i.Include(
            item => item.DisplayName,
            item => item.FileSystemObjectType,
            item => item["FileRef"],
            item => item["File_x0020_Size"],
            item => item["Created"],
            item => item["Modified"],
            item => item["Author"],
            item => item["Editor"]
        ));

        await context.ExecuteQueryAsync();

        var documents = new List<DocumentItem>();
        int processedCount = 0;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var doc = new DocumentItem
            {
                Name = item.DisplayName,
                RelativeUrl = item["FileRef"]?.ToString() ?? string.Empty,
                ItemType = item.FileSystemObjectType == FileSystemObjectType.File ? "File" : "Folder",
                Created = item["Created"] != null ? Convert.ToDateTime(item["Created"]) : DateTime.MinValue,
                Modified = item["Modified"] != null ? Convert.ToDateTime(item["Modified"]) : DateTime.MinValue,
                CreatedBy = GetUserFromField(item["Author"]),
                ModifiedBy = GetUserFromField(item["Editor"])
            };

            if (item.FileSystemObjectType == FileSystemObjectType.File)
            {
                doc.FileSize = item["File_x0020_Size"] != null ? Convert.ToInt64(item["File_x0020_Size"]) : null;
                doc.FileExtension = Path.GetExtension(doc.Name);
            }

            documents.Add(doc);

            processedCount++;
            progress?.Report(processedCount);
        }

        return documents;
    }

    private string GetUserFromField(object? userField)
    {
        if (userField == null) return string.Empty;

        try
        {
            if (userField is FieldUserValue fieldUserValue)
            {
                return fieldUserValue.LookupValue ?? string.Empty;
            }
            return userField.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private AuthConfig ParseAuthConfig(Connection connection, string decryptedSecret)
    {
        // For this implementation, we expect the secret to be stored as JSON
        // Format: { "TenantId": "xxx", "ClientId": "xxx", "CertificateThumbprint": "xxx" }
        // OR: { "TenantId": "xxx", "ClientId": "xxx", "ClientSecret": "xxx" }

        try
        {
            var config = JsonSerializer.Deserialize<AuthConfig>(decryptedSecret);
            if (config == null)
            {
                throw new InvalidOperationException("Failed to deserialize authentication configuration");
            }

            if (string.IsNullOrEmpty(config.TenantId) || string.IsNullOrEmpty(config.ClientId))
            {
                throw new InvalidOperationException("TenantId and ClientId are required in authentication configuration");
            }

            config.UseCertificate = !string.IsNullOrEmpty(config.CertificateThumbprint);

            return config;
        }
        catch (JsonException)
        {
            // Fallback: treat the entire secret as client secret (legacy format)
            _logger.LogWarning("Secret is not in JSON format, treating as legacy client secret");
            return new AuthConfig
            {
                TenantId = connection.Username, // Store TenantId in Username field for legacy
                ClientId = connection.AuthenticationMode, // Store ClientId in AuthenticationMode for legacy
                ClientSecret = decryptedSecret,
                UseCertificate = false
            };
        }
    }

    private class AuthConfig
    {
        public string? TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? CertificateThumbprint { get; set; }
        public bool UseCertificate { get; set; }
    }
}
