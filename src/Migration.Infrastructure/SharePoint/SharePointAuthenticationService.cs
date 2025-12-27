using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Azure.Core.Pipeline;
using Microsoft.Identity.Client;
using System.Security.Cryptography.X509Certificates;
using DMS.Migration.Domain.Entities;

namespace DMS.Migration.Infrastructure.SharePoint;

public class SharePointAuthenticationService
{
    private readonly ILogger<SharePointAuthenticationService> _logger;
    private readonly Dictionary<string, TokenCredential> _credentialCache = new();

    public SharePointAuthenticationService(ILogger<SharePointAuthenticationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get access token for SharePoint Online using App-Only authentication with certificate
    /// </summary>
    public async Task<string> GetAccessTokenAsync(
        string tenantId,
        string clientId,
        string certificateThumbprint,
        string siteUrl)
    {
        try
        {
            var credential = GetOrCreateCredential(tenantId, clientId, certificateThumbprint);

            // Extract the resource URL from site URL (e.g., https://contoso.sharepoint.com)
            var uri = new Uri(siteUrl);
            var resource = $"{uri.Scheme}://{uri.Host}";
            var scope = $"{resource}/.default";

            var tokenRequest = new TokenRequestContext(new[] { scope });
            var token = await credential.GetTokenAsync(tokenRequest, CancellationToken.None);

            _logger.LogInformation("Successfully acquired access token for {Resource}", resource);
            return token.Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire access token for {SiteUrl}", siteUrl);
            throw;
        }
    }

    /// <summary>
    /// Get access token using client secret (less secure, for testing only)
    /// </summary>
    public async Task<string> GetAccessTokenWithSecretAsync(
        string tenantId,
        string clientId,
        string clientSecret,
        string siteUrl)
    {
        try
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

            var uri = new Uri(siteUrl);
            var resource = $"{uri.Scheme}://{uri.Host}";
            var scope = $"{resource}/.default";

            var tokenRequest = new TokenRequestContext(new[] { scope });
            var token = await credential.GetTokenAsync(tokenRequest, CancellationToken.None);

            _logger.LogInformation("Successfully acquired access token with client secret for {Resource}", resource);
            return token.Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire access token with secret for {SiteUrl}", siteUrl);
            throw;
        }
    }

    /// <summary>
    /// Get Microsoft Graph access token
    /// </summary>
    public async Task<string> GetGraphAccessTokenAsync(
        string tenantId,
        string clientId,
        string certificateThumbprint)
    {
        try
        {
            var credential = GetOrCreateCredential(tenantId, clientId, certificateThumbprint);
            var tokenRequest = new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" });
            var token = await credential.GetTokenAsync(tokenRequest, CancellationToken.None);

            _logger.LogInformation("Successfully acquired Microsoft Graph access token");
            return token.Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire Microsoft Graph access token");
            throw;
        }
    }

    private TokenCredential GetOrCreateCredential(string tenantId, string clientId, string certificateThumbprint)
    {
        var key = $"{tenantId}_{clientId}_{certificateThumbprint}";

        if (_credentialCache.TryGetValue(key, out var credential))
        {
            return credential;
        }

        // Load certificate from user or machine certificate store
        var cert = LoadCertificateFromStore(certificateThumbprint);
        if (cert == null)
        {
            throw new InvalidOperationException($"Certificate with thumbprint {certificateThumbprint} not found in certificate stores");
        }

        credential = new ClientCertificateCredential(tenantId, clientId, cert);
        _credentialCache[key] = credential;

        _logger.LogInformation("Created new credential for TenantId: {TenantId}, ClientId: {ClientId}", tenantId, clientId);
        return credential;
    }

    private X509Certificate2? LoadCertificateFromStore(string thumbprint)
    {
        // Remove spaces and convert to uppercase for comparison
        thumbprint = thumbprint.Replace(" ", "").ToUpperInvariant();

        // Try Current User store first, then Local Machine
        var stores = new[]
        {
            new { Location = StoreLocation.CurrentUser, Name = StoreName.My },
            new { Location = StoreLocation.LocalMachine, Name = StoreName.My }
        };

        foreach (var storeInfo in stores)
        {
            using var store = new X509Store(storeInfo.Name, storeInfo.Location);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);

                if (certs.Count > 0)
                {
                    _logger.LogInformation("Found certificate in {Location}/{Name} store", storeInfo.Location, storeInfo.Name);
                    return certs[0];
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error accessing {Location}/{Name} certificate store", storeInfo.Location, storeInfo.Name);
            }
        }

        return null;
    }

    /// <summary>
    /// Load certificate from PFX file (for development/testing)
    /// </summary>
    public X509Certificate2? LoadCertificateFromFile(string pfxPath, string? password = null)
    {
        try
        {
            if (!File.Exists(pfxPath))
            {
                _logger.LogError("Certificate file not found: {PfxPath}", pfxPath);
                return null;
            }

            var cert = string.IsNullOrEmpty(password)
                ? new X509Certificate2(pfxPath)
                : new X509Certificate2(pfxPath, password);

            _logger.LogInformation("Loaded certificate from file: {PfxPath}", pfxPath);
            return cert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load certificate from file: {PfxPath}", pfxPath);
            return null;
        }
    }

    /// <summary>
    /// Get Microsoft Graph client for SharePoint operations
    /// </summary>
    public async Task<GraphServiceClient> GetGraphClientAsync(Connection connection)
    {
        try
        {
            // For now, use a simple token credential approach
            // This would need to be enhanced based on your actual authentication setup
            _logger.LogWarning("GetGraphClientAsync called for connection {ConnectionId}. Using placeholder authentication - needs implementation based on your auth model", connection.Id);

            // This is a placeholder - you'll need to implement based on your ConnectionSecret model
            // For testing, you might hardcode credentials or use environment variables
            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ?? "common";
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? throw new InvalidOperationException("AZURE_CLIENT_ID environment variable not set");
            var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") ?? throw new InvalidOperationException("AZURE_CLIENT_SECRET environment variable not set");

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

            // Create token request context callback for Azure.Identity TokenCredential
            var tokenRequestContext = new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" });
            var authProvider = new BaseBearerTokenAuthenticationProvider(new TokenCredentialAccessTokenProvider(credential, tokenRequestContext));
            var graphClient = new GraphServiceClient(authProvider);

            _logger.LogInformation("Successfully created Graph client for connection {ConnectionId}", connection.Id);
            return await Task.FromResult(graphClient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Graph client for connection {ConnectionId}", connection.Id);
            throw;
        }
    }

    /// <summary>
    /// Get Microsoft Graph access token using client secret
    /// </summary>
    public async Task<string> GetGraphAccessTokenWithSecretAsync(
        string tenantId,
        string clientId,
        string clientSecret)
    {
        try
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var tokenRequest = new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" });
            var token = await credential.GetTokenAsync(tokenRequest, CancellationToken.None);

            _logger.LogInformation("Successfully acquired Microsoft Graph access token with client secret");
            return token.Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire Microsoft Graph access token with client secret");
            throw;
        }
    }
}

/// <summary>
/// Token provider that wraps Azure.Identity TokenCredential for Microsoft Graph SDK
/// </summary>
internal class TokenCredentialAccessTokenProvider : IAccessTokenProvider
{
    private readonly TokenCredential _credential;
    private readonly TokenRequestContext _context;

    public TokenCredentialAccessTokenProvider(TokenCredential credential, TokenRequestContext context)
    {
        _credential = credential;
        _context = context;
    }

    public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        var token = await _credential.GetTokenAsync(_context, cancellationToken);
        return token.Token;
    }

    public AllowedHostsValidator AllowedHostsValidator => new AllowedHostsValidator();
}
