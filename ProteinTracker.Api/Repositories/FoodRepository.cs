using Microsoft.EntityFrameworkCore;
using ProteinTracker.Api.Data;
using ProteinTracker.Api.Models;

namespace ProteinTracker.Api.Repositories;

public class FoodRepository(ProteinTrackerDbContext context)
{
    public async Task<List<Food>> GetAllActiveAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await context.Foods
            .AsNoTracking()
            .Where(food => food.UserId == userId && !food.IsArchived)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Food>> GetAllArchivedAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await context.Foods
            .AsNoTracking()
            .Where(food => food.UserId == userId && food.IsArchived)
            .ToListAsync(cancellationToken);
    }

    public async Task<Food?> GetByIdAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await context.Foods
            .AsNoTracking()
            .FirstOrDefaultAsync(food => food.Id == id && food.UserId == userId, cancellationToken);
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
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await context.FoodEntries
            .AsNoTracking()
            .AnyAsync(entry => entry.FoodId == foodId && entry.UserId == userId, cancellationToken);
    }

    public async Task DeleteAsync(
        Food food,
        CancellationToken cancellationToken = default)
    {
        context.Foods.Remove(food);
        await context.SaveChangesAsync(cancellationToken);
    }
}
