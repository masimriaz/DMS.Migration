using Microsoft.EntityFrameworkCore;
using DMS.Migration.Application.Discovery.Interfaces;
using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;
using DMS.Migration.Infrastructure.Data;

namespace DMS.Migration.Infrastructure.Persistence.Repositories;

public class DiscoveryRepository : IDiscoveryRepository
{
    private readonly AppDbContext _context;

    public DiscoveryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DiscoveryRun?> GetRunByIdAsync(Guid runId, int tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.DiscoveryRuns
            .Include(r => r.SourceConnection)
            .Include(r => r.Metrics)
            .FirstOrDefaultAsync(r => r.Id == runId && r.TenantId == tenantId, cancellationToken);
    }

    public async Task<IReadOnlyList<DiscoveryRun>> GetRunsAsync(
        int tenantId,
        DiscoveryStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DiscoveryRuns
            .Include(r => r.SourceConnection)
            .Where(r => r.TenantId == tenantId);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<DiscoveryRun> CreateRunAsync(DiscoveryRun run, CancellationToken cancellationToken = default)
    {
        _context.DiscoveryRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken);
        return run;
    }

    public async Task UpdateRunAsync(DiscoveryRun run, CancellationToken cancellationToken = default)
    {
        _context.DiscoveryRuns.Update(run);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DiscoveryItem>> GetItemsAsync(
        Guid runId,
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DiscoveryItems
            .Where(i => i.DiscoveryRunId == runId && i.TenantId == tenantId)
            .OrderBy(i => i.Path)
            .ToListAsync(cancellationToken);
    }

    public async Task AddItemsAsync(IEnumerable<DiscoveryItem> items, CancellationToken cancellationToken = default)
    {
        await _context.DiscoveryItems.AddRangeAsync(items, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DiscoveryWarning>> GetWarningsAsync(
        Guid runId,
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DiscoveryWarnings
            .Where(w => w.DiscoveryRunId == runId && w.TenantId == tenantId)
            .OrderBy(w => w.DetectedAt)
            .ToListAsync(cancellationToken);
    }
}
