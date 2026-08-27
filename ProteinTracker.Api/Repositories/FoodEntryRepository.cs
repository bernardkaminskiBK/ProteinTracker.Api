using Microsoft.EntityFrameworkCore;
using ProteinTracker.Api.Data;
using ProteinTracker.Api.Models;

namespace ProteinTracker.Api.Repositories;

public class FoodEntryRepository(ProteinTrackerDbContext context)
{
    public async Task<FoodEntry?> GetByIdAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await context.FoodEntries
            .AsNoTracking()
            .Include(entry => entry.Food)
            .FirstOrDefaultAsync(entry => entry.Id == id && entry.UserId == userId, cancellationToken);
    }

    public async Task<List<FoodEntry>> GetByDateRangeAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await context.FoodEntries
            .AsNoTracking()
            .Include(entry => entry.Food)
            .Where(entry => entry.UserId == userId && entry.ConsumedAt >= start && entry.ConsumedAt < end)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        FoodEntry foodEntry,
        CancellationToken cancellationToken = default)
    {
        await context.FoodEntries.AddAsync(foodEntry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        FoodEntry foodEntry,
        CancellationToken cancellationToken = default)
    {
        context.FoodEntries.Update(foodEntry);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        FoodEntry foodEntry,
        CancellationToken cancellationToken = default)
    {
        context.FoodEntries.Remove(foodEntry);
        await context.SaveChangesAsync(cancellationToken);
    }
}
