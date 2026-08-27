using Microsoft.EntityFrameworkCore;
using ProteinTracker.Api.Data;
using ProteinTracker.Api.Models;

namespace ProteinTracker.Api.Repositories;

public class DailyTargetRepository(ProteinTrackerDbContext context)
{
    public async Task<DailyTarget?> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.DailyTargets
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(
        DailyTarget dailyTarget,
        CancellationToken cancellationToken = default)
    {
        await context.DailyTargets.AddAsync(dailyTarget, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        DailyTarget dailyTarget,
        CancellationToken cancellationToken = default)
    {
        context.DailyTargets.Update(dailyTarget);
        await context.SaveChangesAsync(cancellationToken);
    }
}
