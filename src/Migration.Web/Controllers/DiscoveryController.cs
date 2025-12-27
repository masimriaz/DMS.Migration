using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DMS.Migration.Application.Interfaces;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;
using DMS.Migration.Infrastructure.Data;
using DMS.Migration.Infrastructure.Jobs;
using DMS.Migration.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DMS.Migration.Web.Controllers;

[Authorize]
public class DiscoveryController : Controller
{
    private readonly AppDbContext _context;
    private readonly IDiscoveryService _discoveryService;
    private readonly IJobQueue _jobQueue;
    private readonly ILogger<DiscoveryController> _logger;

    // Hardcoded tenant for demo - in production, get from session/claims
    private const int CurrentTenantId = 1;
    private const string CurrentUser = "Admin User";

    public DiscoveryController(
        AppDbContext context,
        IDiscoveryService discoveryService,
        IJobQueue jobQueue,
        ILogger<DiscoveryController> logger)
    {
        _context = context;
        _discoveryService = discoveryService;
        _jobQueue = jobQueue;
        _logger = logger;
    }

    // GET: /Discovery
    [HttpGet]
    public async Task<IActionResult> Index(DiscoveryStatus? status, int page = 1)
    {
        ViewData["Title"] = "Discovery Runs";
        ViewData["ShowBreadcrumb"] = false;

        var runs = await _discoveryService.GetDiscoveryRunsAsync(
            CurrentTenantId,
            status: status,
            pageNumber: page,
            pageSize: 20);

        var viewModel = new DiscoveryIndexViewModel
        {
            Runs = runs,
            CurrentPage = page,
            FilterStatus = status
        };

        return View(viewModel);
    }

    // GET: /Discovery/New
    [HttpGet]
    public async Task<IActionResult> New()
    {
        ViewData["Title"] = "New Discovery Run";
        ViewData["ShowBreadcrumb"] = true;
        ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Discovery'>Discovery</a></li><li class='breadcrumb-item active'>New</li>";

        // Get available source connections
        var connections = await _context.Connections
            .Where(c => c.TenantId == CurrentTenantId
                && !c.IsDeleted
                && c.Role == ConnectionRole.Source
                && (c.Type == ConnectionType.SharePointOnline || c.Type == ConnectionType.SharePointOnPrem))
            .OrderBy(c => c.Name)
            .ToListAsync();

        var viewModel = new NewDiscoveryViewModel
        {
            AvailableConnections = connections
        };

        return View(viewModel);
    }

    // POST: /Discovery/New
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(NewDiscoveryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableConnections = await _context.Connections
                .Where(c => c.TenantId == CurrentTenantId && !c.IsDeleted && c.Role == ConnectionRole.Source)
                .ToListAsync();
            return View(model);
        }

        try
        {
            // Validate connection exists
            var connection = await _context.Connections
                .FirstOrDefaultAsync(c => c.Id == model.SourceConnectionId && c.TenantId == CurrentTenantId);

            if (connection == null)
            {
                ModelState.AddModelError("", "Invalid connection selected");
                return View(model);
            }

            // Build configuration
            var configuration = new Dictionary<string, object>
            {
                ["ScanVersioning"] = model.ScanVersioning,
                ["ScanPermissions"] = model.ScanPermissions,
                ["ScanCheckedOutFiles"] = model.ScanCheckedOutFiles,
                ["ScanCustomPages"] = model.ScanCustomPages,
                ["MaxDepth"] = model.MaxDepth
            };

            // Create discovery run
            var run = await _discoveryService.CreateDiscoveryRunAsync(
                CurrentTenantId,
                model.SourceConnectionId,
                model.RunName,
                model.ScopeUrl,
                configuration,
                CurrentUser);

            // Enqueue background job
            var job = new DiscoveryJob
            {
                DiscoveryRunId = run.Id,
                TenantId = CurrentTenantId
            };

            await _jobQueue.EnqueueAsync("discovery", JsonSerializer.Serialize(job));

            _logger.LogInformation("Discovery run {RunId} created and queued", run.Id);

            TempData["SuccessMessage"] = $"Discovery run '{run.Name}' has been started.";
            return RedirectToAction(nameof(Details), new { id = run.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create discovery run");
            ModelState.AddModelError("", "Failed to start discovery run. Please try again.");

            model.AvailableConnections = await _context.Connections
                .Where(c => c.TenantId == CurrentTenantId && !c.IsDeleted && c.Role == ConnectionRole.Source)
                .ToListAsync();

            return View(model);
        }
    }

    // GET: /Discovery/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var run = await _discoveryService.GetDiscoveryRunAsync(id, CurrentTenantId);
        if (run == null)
            return NotFound();

        ViewData["Title"] = run.Name;
        ViewData["ShowBreadcrumb"] = true;
        ViewData["Breadcrumb"] = $"<li class='breadcrumb-item'><a href='/Discovery'>Discovery</a></li><li class='breadcrumb-item active'>{run.Name}</li>";

        // Get summary metrics
        var metrics = await _discoveryService.GetSummaryMetricsAsync(id, CurrentTenantId);

        // Get warning count
        var warningCount = await _discoveryService.GetWarningCountAsync(id);
        var criticalWarningCount = await _discoveryService.GetWarningCountAsync(id, DiscoverySeverity.Critical);

        var viewModel = new DiscoveryDetailsViewModel
        {
            Run = run,
            SummaryMetrics = metrics,
            TotalWarnings = warningCount,
            CriticalWarnings = criticalWarningCount
        };

        return View(viewModel);
    }

    // GET: /Discovery/{id}/Items
    [HttpGet("{id}/Items")]
    public async Task<IActionResult> Items(Guid id, DiscoveryItemType? type, int page = 1)
    {
        var run = await _discoveryService.GetDiscoveryRunAsync(id, CurrentTenantId);
        if (run == null)
            return NotFound();

        ViewData["Title"] = $"{run.Name} - Items";
        ViewData["ShowBreadcrumb"] = true;
        ViewData["Breadcrumb"] = $"<li class='breadcrumb-item'><a href='/Discovery'>Discovery</a></li>" +
                                 $"<li class='breadcrumb-item'><a href='/Discovery/{id}'>{run.Name}</a></li>" +
                                 $"<li class='breadcrumb-item active'>Items</li>";

        var items = await _discoveryService.GetDiscoveryItemsAsync(
            id,
            CurrentTenantId,
            itemType: type,
            pageNumber: page,
            pageSize: 50);

        var totalItems = await _discoveryService.GetItemCountAsync(id, type);

        var viewModel = new DiscoveryItemsViewModel
        {
            Run = run,
            Items = items,
            FilterType = type,
            CurrentPage = page,
            PageSize = 50,
            TotalItems = totalItems
        };

        return View(viewModel);
    }

    // GET: /Discovery/{id}/Warnings
    [HttpGet("{id}/Warnings")]
    public async Task<IActionResult> Warnings(Guid id, DiscoverySeverity? severity, int page = 1)
    {
        var run = await _discoveryService.GetDiscoveryRunAsync(id, CurrentTenantId);
        if (run == null)
            return NotFound();

        ViewData["Title"] = $"{run.Name} - Warnings";
        ViewData["ShowBreadcrumb"] = true;
        ViewData["Breadcrumb"] = $"<li class='breadcrumb-item'><a href='/Discovery'>Discovery</a></li>" +
                                 $"<li class='breadcrumb-item'><a href='/Discovery/{id}'>{run.Name}</a></li>" +
                                 $"<li class='breadcrumb-item active'>Warnings</li>";

        var warnings = await _discoveryService.GetWarningsAsync(
            id,
            CurrentTenantId,
            minSeverity: severity,
            pageNumber: page,
            pageSize: 50);

        var totalWarnings = await _discoveryService.GetWarningCountAsync(id, severity);

        var viewModel = new DiscoveryWarningsViewModel
        {
            Run = run,
            Warnings = warnings,
            FilterSeverity = severity,
            CurrentPage = page,
            PageSize = 50,
            TotalWarnings = totalWarnings
        };

        return View(viewModel);
    }

    // GET: /Discovery/{id}/Export
    [HttpGet("{id}/Export")]
    public async Task<IActionResult> Export(Guid id, DiscoveryExportFormat format = DiscoveryExportFormat.Json,
        DiscoveryExportType exportType = DiscoveryExportType.Summary)
    {
        var run = await _discoveryService.GetDiscoveryRunAsync(id, CurrentTenantId);
        if (run == null)
            return NotFound();

        try
        {
            // Create export record
            var export = await _discoveryService.CreateExportAsync(
                id,
                CurrentTenantId,
                format,
                exportType,
                CurrentUser);

            // Generate export data
            var data = await _discoveryService.GenerateExportDataAsync(export);

            // Update export record
            export.FileSizeBytes = data.Length;
            export.IsDownloaded = true;
            export.DownloadCount++;
            await _context.SaveChangesAsync();

            var contentType = format switch
            {
                DiscoveryExportFormat.Json => "application/json",
                DiscoveryExportFormat.Csv => "text/csv",
                _ => "application/octet-stream"
            };

            return File(data, contentType, export.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate export for run {RunId}", id);
            TempData["ErrorMessage"] = "Failed to generate export. Please try again.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // GET: /Discovery/{id}/Status (AJAX endpoint for polling)
    [HttpGet("{id}/Status")]
    public async Task<IActionResult> Status(Guid id)
    {
        var run = await _discoveryService.GetDiscoveryRunAsync(id, CurrentTenantId);
        if (run == null)
            return NotFound();

        return Json(new
        {
            run.Status,
            run.ProgressPercentage,
            run.CurrentStep,
            run.ErrorMessage,
            IsComplete = run.Status == DiscoveryStatus.Completed ||
                        run.Status == DiscoveryStatus.Failed ||
                        run.Status == DiscoveryStatus.Cancelled
        });
    }

    // POST: /Discovery/{id}/Cancel
    [HttpPost("{id}/Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var success = await _discoveryService.CancelDiscoveryRunAsync(id);

        if (!success)
            return NotFound();

        TempData["SuccessMessage"] = "Discovery run cancelled.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
