using Microsoft.EntityFrameworkCore;
using ProteinTracker.Api.Data;
using ProteinTracker.Api.Models;

namespace ProteinTracker.Api.Repositories;

public class FoodRepository(ProteinTrackerDbContext context)
{
    public async Task<List<Food>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.Foods
            .AsNoTracking()
            .Where(food => !food.IsArchived)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Food>> GetAllArchivedAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.Foods
            .AsNoTracking()
            .Where(food => food.IsArchived)
            .ToListAsync(cancellationToken);
    }

    public async Task<Food?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await context.Foods
            .AsNoTracking()
            .FirstOrDefaultAsync(food => food.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        Food food,
        CancellationToken cancellationToken = default)
    {
        await context.Foods.AddAsync(food, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Food food,
        CancellationToken cancellationToken = default)
    {
        context.Foods.Update(food);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasFoodEntriesAsync(
        int foodId,
        CancellationToken cancellationToken = default)
    {
        return await context.FoodEntries
            .AsNoTracking()
            .AnyAsync(entry => entry.FoodId == foodId, cancellationToken);
    }

    public async Task DeleteAsync(
        Food food,
        CancellationToken cancellationToken = default)
    {
        context.Foods.Remove(food);
        await context.SaveChangesAsync(cancellationToken);
    }
}
