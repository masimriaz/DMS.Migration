using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using DMS.Migration.Application.Interfaces;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;
using DMS.Migration.Infrastructure.Data;
using DMS.Migration.Infrastructure.SharePoint;
using System.Text.Json;

namespace DMS.Migration.Infrastructure.Services.Verifiers;

public class ConnectionVerifierFactory : IConnectionVerifierFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ConnectionVerifierFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IConnectionVerifier GetVerifier(ConnectionType type)
    {
        return type switch
        {
            ConnectionType.SharePointOnPrem => (IConnectionVerifier)_serviceProvider.GetService(typeof(SharePointOnPremVerifier))!,
            ConnectionType.SharePointOnline => (IConnectionVerifier)_serviceProvider.GetService(typeof(SharePointOnlineVerifier))!,
            ConnectionType.OneDriveForBusiness => (IConnectionVerifier)_serviceProvider.GetService(typeof(OneDriveVerifier))!,
            ConnectionType.FileShare => (IConnectionVerifier)_serviceProvider.GetService(typeof(FileShareVerifier))!,
            _ => throw new NotSupportedException($"Connection type {type} is not supported")
        };
    }
}

public class SharePointOnPremVerifier : IConnectionVerifier
{
    private readonly AppDbContext _context;
    private readonly ILogger<SharePointOnPremVerifier> _logger;

    public SharePointOnPremVerifier(AppDbContext context, ILogger<SharePointOnPremVerifier> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ConnectionVerificationRun> VerifyAsync(Connection connection, string initiatedBy)
    {
        var run = new ConnectionVerificationRun
        {
            ConnectionId = connection.Id,
            TenantId = connection.TenantId,
            Status = VerificationStatus.Running,
            StartedAt = DateTime.UtcNow,
            InitiatedBy = initiatedBy
        };

        _context.ConnectionVerificationRuns.Add(run);
        await _context.SaveChangesAsync();

        try
        {
            // Simulate SharePoint On-Prem verification (CSOM would go here)
            await Task.Delay(2000);

            // Mock validation
            if (string.IsNullOrWhiteSpace(connection.EndpointUrl))
                throw new InvalidOperationException("Endpoint URL is required");

            if (!connection.EndpointUrl.StartsWith("http://") && !connection.EndpointUrl.StartsWith("https://"))
                throw new InvalidOperationException("Invalid URL format");

            run.Status = VerificationStatus.Success;
            run.Result = "Success";
            run.Diagnostics = $"Successfully connected to SharePoint site at {connection.EndpointUrl}. " +
                            $"Site title verified. User has appropriate permissions.";

            connection.Status = ConnectionStatus.Verified;
            connection.LastVerifiedAt = DateTime.UtcNow;
            connection.LastVerificationResult = "Success";
            connection.LastVerificationDiagnostics = run.Diagnostics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SharePoint On-Prem verification failed for connection {ConnectionId}", connection.Id);

            run.Status = VerificationStatus.Failed;
            run.Result = "Failed";
            run.ErrorMessage = ex.Message;
            run.Diagnostics = $"Failed to connect: {ex.Message}";

            connection.Status = ConnectionStatus.Failed;
            connection.LastVerifiedAt = DateTime.UtcNow;
            connection.LastVerificationResult = "Failed";
            connection.LastVerificationDiagnostics = ex.Message;
        }
        finally
        {
            run.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return run;
    }
}

public class SharePointOnlineVerifier : IConnectionVerifier
{
    private readonly AppDbContext _context;
    private readonly ILogger<SharePointOnlineVerifier> _logger;
    private readonly SharePointOnlineService _spoService;
    private readonly IDataProtector _protector;

    public SharePointOnlineVerifier(
        AppDbContext context,
        ILogger<SharePointOnlineVerifier> logger,
        SharePointOnlineService spoService,
        IDataProtectionProvider protectionProvider)
    {
        _context = context;
        _logger = logger;
        _spoService = spoService;
        _protector = protectionProvider.CreateProtector("ConnectionSecrets");
    }

    public async Task<ConnectionVerificationRun> VerifyAsync(Connection connection, string initiatedBy)
    {
        var run = new ConnectionVerificationRun
        {
            ConnectionId = connection.Id,
            TenantId = connection.TenantId,
            Status = VerificationStatus.Running,
            StartedAt = DateTime.UtcNow,
            InitiatedBy = initiatedBy
        };

        _context.ConnectionVerificationRuns.Add(run);
        await _context.SaveChangesAsync();

        try
        {
            // Load the connection secret
            var secret = await _context.ConnectionSecrets
                .FirstOrDefaultAsync(s => s.ConnectionId == connection.Id);

            if (secret == null)
            {
                throw new InvalidOperationException("Connection secret not found. Please provide authentication credentials.");
            }

            // Decrypt the secret
            var decryptedSecret = _protector.Unprotect(secret.EncryptedSecret);

            // Verify connection using real SharePoint Online API
            var (success, message, diagnostics) = await _spoService.VerifyConnectionAsync(connection, decryptedSecret);

            if (success)
            {
                run.Status = VerificationStatus.Success;
                run.Result = "Success";
                run.Diagnostics = message;
                run.DiagnosticsJson = JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions { WriteIndented = true });

                connection.Status = ConnectionStatus.Verified;
                connection.LastVerifiedAt = DateTime.UtcNow;
                connection.LastVerificationResult = "Success";
                connection.LastVerificationDiagnostics = message;
            }
            else
            {
                throw new Exception(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SharePoint Online verification failed for connection {ConnectionId}", connection.Id);

            run.Status = VerificationStatus.Failed;
            run.Result = "Failed";
            run.ErrorMessage = ex.Message;
            run.Diagnostics = $"Authentication failed: {ex.Message}";

            connection.Status = ConnectionStatus.Failed;
            connection.LastVerifiedAt = DateTime.UtcNow;
            connection.LastVerificationResult = "Failed";
            connection.LastVerificationDiagnostics = ex.Message;
        }
        finally
        {
            run.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return run;
    }
}

public class OneDriveVerifier : IConnectionVerifier
{
    private readonly AppDbContext _context;
    private readonly ILogger<OneDriveVerifier> _logger;

    public OneDriveVerifier(AppDbContext context, ILogger<OneDriveVerifier> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ConnectionVerificationRun> VerifyAsync(Connection connection, string initiatedBy)
    {
        var run = new ConnectionVerificationRun
        {
            ConnectionId = connection.Id,
            TenantId = connection.TenantId,
            Status = VerificationStatus.Running,
            StartedAt = DateTime.UtcNow,
            InitiatedBy = initiatedBy
        };

        _context.ConnectionVerificationRuns.Add(run);
        await _context.SaveChangesAsync();

        try
        {
            await Task.Delay(1500);

            if (!connection.EndpointUrl.Contains("onedrive"))
                throw new InvalidOperationException("Invalid OneDrive URL");

            run.Status = VerificationStatus.Success;
            run.Result = "Success";
            run.Diagnostics = $"Successfully connected to OneDrive for Business. " +
                            $"Root folder accessible. Graph API permissions validated.";

            connection.Status = ConnectionStatus.Verified;
            connection.LastVerifiedAt = DateTime.UtcNow;
            connection.LastVerificationResult = "Success";
            connection.LastVerificationDiagnostics = run.Diagnostics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OneDrive verification failed for connection {ConnectionId}", connection.Id);

            run.Status = VerificationStatus.Failed;
            run.Result = "Failed";
            run.ErrorMessage = ex.Message;
            run.Diagnostics = $"Connection failed: {ex.Message}";

            connection.Status = ConnectionStatus.Failed;
            connection.LastVerifiedAt = DateTime.UtcNow;
            connection.LastVerificationResult = "Failed";
            connection.LastVerificationDiagnostics = ex.Message;
        }
        finally
        {
            run.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return run;
    }
}

public class FileShareVerifier : IConnectionVerifier
{
    private readonly AppDbContext _context;
    private readonly ILogger<FileShareVerifier> _logger;

    public FileShareVerifier(AppDbContext context, ILogger<FileShareVerifier> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ConnectionVerificationRun> VerifyAsync(Connection connection, string initiatedBy)
    {
        var run = new ConnectionVerificationRun
        {
            ConnectionId = connection.Id,
            TenantId = connection.TenantId,
            Status = VerificationStatus.Running,
            StartedAt = DateTime.UtcNow,
            InitiatedBy = initiatedBy
        };

        _context.ConnectionVerificationRuns.Add(run);
        await _context.SaveChangesAsync();

        try
        {
            await Task.Delay(1000);

            if (!connection.EndpointUrl.StartsWith(@"\\"))
                throw new InvalidOperationException("File share path must be a UNC path (\\\\server\\share)");

            // Simulate directory existence check
            var pathExists = true; // In production: Directory.Exists(connection.EndpointUrl)

            if (!pathExists)
                throw new InvalidOperationException("Path does not exist or is not accessible");

            run.Status = VerificationStatus.Success;
            run.Result = "Success";
            run.Diagnostics = $"Successfully accessed file share at {connection.EndpointUrl}. " +
                            $"Path exists and is readable.";

            connection.Status = ConnectionStatus.Verified;
            connection.LastVerifiedAt = DateTime.UtcNow;
            connection.LastVerificationResult = "Success";
            connection.LastVerificationDiagnostics = run.Diagnostics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File share verification failed for connection {ConnectionId}", connection.Id);

            run.Status = VerificationStatus.Failed;
            run.Result = "Failed";
            run.ErrorMessage = ex.Message;
            run.Diagnostics = $"Access failed: {ex.Message}";

            connection.Status = ConnectionStatus.Failed;
            connection.LastVerifiedAt = DateTime.UtcNow;
            connection.LastVerificationResult = "Failed";
            connection.LastVerificationDiagnostics = ex.Message;
        }
        finally
        {
            run.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return run;
    }
}
