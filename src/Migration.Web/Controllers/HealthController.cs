using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Reflection;
using DMS.Migration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DMS.Migration.Web.Controllers;

[Route("[controller]")]
[AllowAnonymous]
public class HealthController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public HealthController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    /// <summary>
    /// Basic health check endpoint for Docker health monitoring
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        try
        {
            // Check database connectivity
            var canConnect = _context.Database.CanConnect();

            if (!canConnect)
            {
                return StatusCode(503, new { status = "Unhealthy", reason = "Database not accessible" });
            }

            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                environment = _env.EnvironmentName,
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { status = "Unhealthy", error = ex.Message });
        }
    }

    /// <summary>
    /// Detailed health check with system information
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> Detailed()
    {
        if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            return Unauthorized();

        try
        {
            var process = Process.GetCurrentProcess();
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();

            // Database stats
            var connectionCount = await _context.Connections.CountAsync();
            var jobCount = await _context.MigrationJobs.CountAsync();
            var tenantCount = await _context.Tenants.CountAsync();

            var healthData = new
            {
                Application = new
                {
                    Name = "DMS SharePoint Migration Tool",
                    Version = assemblyName.Version?.ToString() ?? "1.0.0",
                    Environment = _env.EnvironmentName,
                    ContentRootPath = _env.ContentRootPath,
                    WebRootPath = _env.WebRootPath,
                    StartTime = Process.GetCurrentProcess().StartTime,
                    Uptime = DateTime.Now - Process.GetCurrentProcess().StartTime
                },
                System = new
                {
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                    Is64BitProcess = Environment.Is64BitProcess,
                    RuntimeVersion = Environment.Version.ToString(),
                    WorkingSet = $"{process.WorkingSet64 / 1024 / 1024} MB",
                    PrivateMemory = $"{process.PrivateMemorySize64 / 1024 / 1024} MB",
                    ProcessId = process.Id
                },
                Docker = new
                {
                    IsRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true",
                    ContainerHostname = Environment.MachineName
                },
                Database = new
                {
                    Provider = "InMemory",
                    Connected = _context.Database.CanConnect(),
                    Connections = connectionCount,
                    Jobs = jobCount,
                    Tenants = tenantCount
                },
                Health = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow
                }
            };

            return Json(healthData);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "Error", message = ex.Message });
        }
    }
}
