using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Hosting;
using DMS.Migration.Application.Common.Abstractions;
using DMS.Migration.Application.Connections.Interfaces;
using DMS.Migration.Application.Discovery.Interfaces;
using DMS.Migration.Application.Interfaces;
using DMS.Migration.Application.Jobs.Interfaces;
using DMS.Migration.Infrastructure.Data;
using DMS.Migration.Infrastructure.Jobs;
using DMS.Migration.Infrastructure.Persistence.Repositories;
using DMS.Migration.Infrastructure.Services;
using DMS.Migration.Infrastructure.Services.Verifiers;
using DMS.Migration.Infrastructure.SharePoint;

namespace DMS.Migration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // ============================================================
        // DATABASE CONFIGURATION - PostgreSQL
        // ============================================================
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            });

            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        // ============================================================
        // CORE ABSTRACTIONS
        // ============================================================
        services.AddScoped<ITenantContext, TenantContextService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();

        // ============================================================
        // AUTHENTICATION SERVICES
        // ============================================================
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDevSeedService, DevSeedService>();

        // ============================================================
        // REPOSITORIES
        // ============================================================
        services.AddScoped<IConnectionRepository, ConnectionRepository>();
        services.AddScoped<IDiscoveryRepository, DiscoveryRepository>();

        // ============================================================
        // LEGACY APPLICATION SERVICES (to be migrated)
        // ============================================================
        services.AddScoped<IConnectionService, Services.ConnectionService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IDiscoveryService, Services.DiscoveryService>();

        // ============================================================
        // CONNECTION VERIFIERS
        // ============================================================
        // Old interface (for legacy code)
        services.AddScoped<Application.Interfaces.IConnectionVerifierFactory, Services.Verifiers.ConnectionVerifierFactory>();
        // New interface (for refactored code)
        services.AddScoped<Application.Connections.Interfaces.IConnectionVerifierFactory, Services.Verifiers.NewConnectionVerifierFactory>();

        services.AddScoped<SharePointOnPremVerifier>();
        services.AddScoped<SharePointOnlineVerifier>();
        services.AddScoped<OneDriveVerifier>();
        services.AddScoped<FileShareVerifier>();

        // ============================================================
        // SHAREPOINT SERVICES
        // ============================================================
        services.AddSingleton<SharePointAuthenticationService>();
        services.AddScoped<ISharePointClient, SharePointOnlineService>();
        services.AddScoped<SharePointOnlineService>();

        // Discovery scanner
        // Old interface (for legacy code)
        services.AddScoped<Application.Interfaces.IDiscoveryScanner, Services.Scanners.SharePointOnlineDiscoveryScanner>();
        // New interface (for refactored code)
        services.AddScoped<Application.Discovery.Interfaces.IDiscoveryScanner, Services.Scanners.NewDiscoveryScanner>();

        // ============================================================
        // BACKGROUND JOBS
        // ============================================================
        services.AddSingleton<IJobQueue, ChannelJobQueue>();
        services.AddScoped<IRetryPolicy, RetryPolicy>();
        services.AddScoped<IJobExecutor<DiscoveryJob>, DiscoveryJobExecutor>();
        services.AddHostedService<JobWorker>();

        // ============================================================
        // HEALTH CHECKS
        // ============================================================
        services.AddHealthChecks()
            .AddNpgSql(
                connectionString!,
                name: "postgres",
                timeout: TimeSpan.FromSeconds(5),
                tags: new[] { "db", "postgresql" });

        return services;
    }
}
