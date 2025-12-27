using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Reflection;
using DMS.Migration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using DMS.Migration.Web.Models;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Web.Controllers;

[Authorize]
public class SystemInfoController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SystemInfoController> _logger;

    public SystemInfoController(
        AppDbContext context,
        IWebHostEnvironment env,
        ILogger<SystemInfoController> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            return RedirectToAction("Login", "Home");

        try
        {
            var process = Process.GetCurrentProcess();
            var assembly = Assembly.GetExecutingAssembly();

            // Database stats
            var connectionCount = await _context.Connections.CountAsync();
            var activeConnections = await _context.Connections.CountAsync(c => !c.IsDeleted && c.Status == ConnectionStatus.Verified);
            var jobCount = await _context.MigrationJobs.CountAsync();
            var tenantCount = await _context.Tenants.CountAsync();

            var model = new SystemInfoViewModel
            {
                // Application Info
                ApplicationName = "DMS SharePoint Migration Tool",
                Version = assembly.GetName().Version?.ToString() ?? "1.0.0",
                Environment = _env.EnvironmentName,
                StartTime = process.StartTime,
                Uptime = DateTime.Now - process.StartTime,

                // System Info
                MachineName = Environment.MachineName,
                OSVersion = Environment.OSVersion.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                Is64BitOS = Environment.Is64BitOperatingSystem,
                RuntimeVersion = Environment.Version.ToString(),

                // Process Info
                ProcessId = process.Id,
                WorkingSetMB = process.WorkingSet64 / 1024 / 1024,
                PrivateMemoryMB = process.PrivateMemorySize64 / 1024 / 1024,
                ThreadCount = process.Threads.Count,

                // Docker Info
                IsRunningInDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true",
                DockerHostname = Environment.MachineName,

                // Database Stats
                TotalConnections = connectionCount,
                ActiveConnections = activeConnections,
                TotalJobs = jobCount,
                TotalTenants = tenantCount,

                // Paths
                ContentRoot = _env.ContentRootPath,
                WebRoot = _env.WebRootPath
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system information");
            return View("Error");
        }
    }
}
