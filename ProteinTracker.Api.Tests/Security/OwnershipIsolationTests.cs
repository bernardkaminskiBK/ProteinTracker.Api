using Microsoft.EntityFrameworkCore;
using ProteinTracker.Api.Data;
using ProteinTracker.Api.DTOs;
using ProteinTracker.Api.Exceptions;
using ProteinTracker.Api.Models;
using ProteinTracker.Api.Repositories;
using ProteinTracker.Api.Security;
using ProteinTracker.Api.Services;
using Xunit;

namespace ProteinTracker.Api.Tests.Security;

public class OwnershipIsolationTests
{
    [Fact(DisplayName = "Users can list their own foods but cannot access another user's foods")]
    public async Task FoodOperations_AreScopedToCurrentUser()
    {
        await using var context = CreateContext();
        var userAService = FoodService(context, 1);
        var userBService = FoodService(context, 2);
        var food = await userAService.CreateAsync(FoodRequest("A oats"));
        context.ChangeTracker.Clear();

        Assert.Single(await userAService.GetAllActiveAsync());
        Assert.Empty(await userBService.GetAllActiveAsync());
        await Assert.ThrowsAsync<FoodNotFoundException>(() => userBService.GetByIdAsync(food.Id));
        await Assert.ThrowsAsync<FoodNotFoundException>(() => userBService.UpdateAsync(food.Id, FoodUpdate("Changed")));
        await Assert.ThrowsAsync<FoodNotFoundException>(() => userBService.ArchiveAsync(food.Id));
        await Assert.ThrowsAsync<FoodNotFoundException>(() => userBService.DeleteAsync(food.Id));
    }

    [Fact(DisplayName = "A user cannot create or reassign an entry with another user's food")]
    public async Task FoodEntryAssignments_RequireAnOwnedFood()
    {
        await using var context = CreateContext();
        var userAFood = await FoodService(context, 1).CreateAsync(FoodRequest("A oats"));
        var userBFood = await FoodService(context, 2).CreateAsync(FoodRequest("B oats"));
        context.ChangeTracker.Clear();
        var userBEntries = FoodEntryService(context, 2);

        await Assert.ThrowsAsync<FoodNotFoundException>(() => userBEntries.CreateAsync(new CreateFoodEntryRequest
        {
            FoodId = userAFood.Id,
            AmountInGrams = 100m,
            ConsumedAt = DateTimeOffset.UtcNow
        }));

        var ownEntry = await userBEntries.CreateAsync(new CreateFoodEntryRequest
        {
            FoodId = userBFood.Id,
            AmountInGrams = 100m,
            ConsumedAt = DateTimeOffset.UtcNow
        });
        await Assert.ThrowsAsync<FoodNotFoundException>(() => userBEntries.UpdateAsync(
            ownEntry.Id,
            new UpdateFoodEntryRequest
            {
                FoodId = userAFood.Id,
                AmountInGrams = 120m,
                ConsumedAt = DateTimeOffset.UtcNow
            }));
    }

    [Fact(DisplayName = "Daily targets are isolated per user")]
    public async Task DailyTargets_ArePerUser()
    {
        await using var context = CreateContext();
        var targetA = DailyTargetService(context, 1);
        var targetB = DailyTargetService(context, 2);

        await targetA.UpdateAsync(new UpdateDailyTargetRequest
        {
            ProteinTarget = 160m,
            CarbohydratesTarget = 240m,
            FatTarget = 90m
        });

        Assert.Equal(160m, (await targetA.GetCurrentAsync()).ProteinTarget);
        Assert.Equal(0m, (await targetB.GetCurrentAsync()).ProteinTarget);

        await targetB.UpdateAsync(new UpdateDailyTargetRequest
        {
            ProteinTarget = 100m,
            CarbohydratesTarget = 150m,
            FatTarget = 60m
        });

        Assert.Equal(2, await context.DailyTargets.CountAsync());
    }

    [Fact(DisplayName = "Daily summaries include only the current user's entries and target")]
    public async Task DailySummary_IsScopedToCurrentUser()
    {
        await using var context = CreateContext();
        var date = new DateOnly(2026, 8, 27);
        var foodA = await FoodService(context, 1).CreateAsync(FoodRequest("A oats"));
        var foodB = await FoodService(context, 2).CreateAsync(FoodRequest("B oats"));
        await FoodEntryService(context, 1).CreateAsync(EntryRequest(foodA.Id, 100m));
        await FoodEntryService(context, 2).CreateAsync(EntryRequest(foodB.Id, 200m));
        await DailyTargetService(context, 1).UpdateAsync(TargetRequest(100m));
        await DailyTargetService(context, 2).UpdateAsync(TargetRequest(200m));

        var summaryA = await DailySummaryService(context, 1).GetAsync(date);
        var summaryB = await DailySummaryService(context, 2).GetAsync(date);

        Assert.Equal(7m, summaryA.Consumed.Protein);
        Assert.Equal(14m, summaryB.Consumed.Protein);
        Assert.Equal(100m, summaryA.Target.Protein);
        Assert.Equal(200m, summaryB.Target.Protein);
    }

    [Fact(DisplayName = "Archive and permanent-delete rules remain scoped to the food owner")]
    public async Task ArchiveAndDeleteRules_RemainOwned()
    {
        await using var context = CreateContext();
        var userA = FoodService(context, 1);
        var userB = FoodService(context, 2);
        var food = await userA.CreateAsync(FoodRequest("A oats"));
        await FoodEntryService(context, 1).CreateAsync(EntryRequest(food.Id, 100m));
        context.ChangeTracker.Clear();
        await userA.ArchiveAsync(food.Id);

        await Assert.ThrowsAsync<FoodNotFoundException>(() => userB.RestoreAsync(food.Id));
        await Assert.ThrowsAsync<FoodDeletionConflictException>(() => userA.DeleteAsync(food.Id));
    }

    private static FoodService FoodService(ProteinTrackerDbContext context, int userId) =>
        new(new FoodRepository(context), new CurrentUser(userId));

    private static FoodEntryService FoodEntryService(ProteinTrackerDbContext context, int userId) =>
        new(new FoodEntryRepository(context), new FoodRepository(context), new CurrentUser(userId));

    private static DailyTargetService DailyTargetService(ProteinTrackerDbContext context, int userId) =>
        new(new DailyTargetRepository(context), new CurrentUser(userId));

    private static DailySummaryService DailySummaryService(ProteinTrackerDbContext context, int userId) =>
        new(
            new FoodEntryRepository(context),
            new DailyTargetRepository(context),
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Bratislava"),
            new CurrentUser(userId));

    private static CreateFoodRequest FoodRequest(string name) => new()
    {
        Name = name,
        ProteinPer100g = 7m,
        CarbohydratesPer100g = 77m,
        FatPer100g = 1m
    };

    private static UpdateFoodRequest FoodUpdate(string name) => new()
    {
        Name = name,
        ProteinPer100g = 7m,
        CarbohydratesPer100g = 77m,
        FatPer100g = 1m
    };

    private static CreateFoodEntryRequest EntryRequest(int foodId, decimal amount) => new()
    {
        FoodId = foodId,
        AmountInGrams = amount,
        ConsumedAt = new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero)
    };

    private static UpdateDailyTargetRequest TargetRequest(decimal protein) => new()
    {
        ProteinTarget = protein,
        CarbohydratesTarget = 0m,
        FatTarget = 0m
    };

    private static ProteinTrackerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProteinTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProteinTrackerDbContext(options);
    }
}
