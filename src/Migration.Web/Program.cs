using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DMS.Migration.Application;
using DMS.Migration.Infrastructure;
using DMS.Migration.Infrastructure.Data;
using DMS.Migration.Infrastructure.Services;
using DMS.Migration.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// LOGGING CONFIGURATION
// ============================================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add structured logging scope with correlation ID
builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.ParentId;
});

// ============================================================
// MVC & SESSION
// ============================================================
builder.Services.AddControllersWithViews();

// Add session support (used for supplementary data, auth uses cookies)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".DMSMigration.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP for development
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ============================================================
// AUTHENTICATION & AUTHORIZATION
// ============================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP in development
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = ".DMSMigration.Auth";
    });

builder.Services.AddAuthorization();

// Data Protection for encryption - persist keys to avoid session loss
var keysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("DMSMigration");

// ============================================================
// CLEAN ARCHITECTURE LAYER REGISTRATION
// ============================================================
builder.Services.AddHttpContextAccessor(); // Required for ITenantContext
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

// ============================================================
// DATABASE INITIALIZATION
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<AppDbContext>();

    try
    {
        logger.LogInformation("🔄 Checking database connectivity...");

        var canConnect = await context.Database.CanConnectAsync();
        if (canConnect)
        {
            logger.LogInformation("✅ Database DMS connection verified");

            // Test query to verify tables exist
            var tenantCount = await context.Tenants.CountAsync();
            logger.LogInformation("📊 Found {TenantCount} tenant(s) in database", tenantCount);

            // Seed development data (only in Development)
            if (app.Environment.IsDevelopment())
            {
                var seedService = services.GetRequiredService<IDevSeedService>();
                await seedService.SeedDevelopmentDataAsync();
            }
        }
        else
        {
            logger.LogError("❌ Cannot connect to database DMS!");
            throw new Exception("Database connection failed. Please ensure PostgreSQL is running and database 'DMS' exists.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Database initialization error: {Message}", ex.Message);

        if (app.Environment.IsDevelopment())
        {
            logger.LogError("💡 Tip: Run the database-setup.sql script to create the DMS database");
            logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
        }

        throw; // Stop application startup on database errors
    }
}

// ============================================================
// MIDDLEWARE PIPELINE - ORDER MATTERS!
// ============================================================

// 1. Correlation ID (must be first to track all requests)
app.UseMiddleware<CorrelationIdMiddleware>();

// 2. Request logging
app.UseMiddleware<RequestLoggingMiddleware>();

// 3. Global exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 4. Standard ASP.NET Core middleware
if (!app.Environment.IsDevelopment())
{
    // Don't use built-in exception handler; we have custom middleware
    // app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Don't redirect HTTP to HTTPS in development (interferes with session cookies)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Session must come after Authentication
app.UseSession();

// Health check endpoint
app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("🚀 Data Migration Platform started successfully!");
startupLogger.LogInformation("📊 Environment: {Environment}", app.Environment.EnvironmentName);
startupLogger.LogInformation("🗄️  Database: PostgreSQL");

app.Run();