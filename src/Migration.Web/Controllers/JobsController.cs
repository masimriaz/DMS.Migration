using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DMS.Migration.Infrastructure.Data;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;
using DMS.Migration.Application.Interfaces;

namespace DMS.Migration.Web.Controllers
{
    [Authorize]
    public class JobsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IJobQueue _queue;

        public JobsController(AppDbContext context, IJobQueue queue)
        {
            _context = context;
            _queue = queue;
        }

        public async Task<IActionResult> Index()
        {
            var jobs = await _context.MigrationJobs.OrderByDescending(j => j.CreatedAt).ToListAsync();
            return View(jobs);
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuickJob()
        {
            var job = new MigrationJob
            {
                Name = $"Discovery Scan - {DateTime.Now:HH:mm}",
                Type = JobType.Discovery,
                Status = JobStatus.Queued
            };

            _context.MigrationJobs.Add(job);
            await _context.SaveChangesAsync();

            // Push to background worker
            await _queue.EnqueueJobAsync(job.Id);

            return RedirectToAction(nameof(Index));
        }
    }
}
