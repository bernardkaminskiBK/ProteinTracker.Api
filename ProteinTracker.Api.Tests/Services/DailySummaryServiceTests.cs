using Microsoft.EntityFrameworkCore;
using ProteinTracker.Api.Data;
using ProteinTracker.Api.Models;
using ProteinTracker.Api.Repositories;
using ProteinTracker.Api.Services;
using Xunit;

namespace ProteinTracker.Api.Tests.Services;

public class DailySummaryServiceTests
{
    private static readonly DateOnly SummaryDate = new(2026, 8, 26);
    private static readonly TimeZoneInfo BratislavaTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Bratislava");

    [Fact(DisplayName = "A day without entries has zero consumed nutrition")]
    public async Task GetAsync_WithoutEntries_ReturnsZeroConsumedValues()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var response = await service.GetAsync(SummaryDate);

        Assert.Equal(0m, response.Consumed.Protein);
        Assert.Equal(0m, response.Consumed.Carbohydrates);
        Assert.Equal(0m, response.Consumed.Fat);
        Assert.Equal(0m, response.Consumed.Calories);
    }

    [Fact(DisplayName = "A missing daily target produces zero target values without persistence")]
    public async Task GetAsync_WithoutTarget_ReturnsZeroTargetValues()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var response = await service.GetAsync(SummaryDate);

        Assert.Equal(0m, response.Target.Protein);
        Assert.Equal(0m, response.Target.Carbohydrates);
        Assert.Equal(0m, response.Target.Fat);
        Assert.Equal(0m, response.Target.Calories);
        Assert.Empty(await context.DailyTargets.ToListAsync());
    }

    [Fact(DisplayName = "Multiple food entries are summed into consumed nutrition")]
    public async Task GetAsync_WithMultipleEntries_SumsConsumedNutrition()
    {
        await using var context = CreateContext();
        var oats = await SeedFoodAsync(context, "Oats", 7m, 77m, 1m);
        var chicken = await SeedFoodAsync(context, "Chicken", 30m, 0m, 5m);
        await SeedEntryAsync(context, oats.Id, 100m, UtcInsideSummaryDay(8));
        await SeedEntryAsync(context, chicken.Id, 200m, UtcInsideSummaryDay(12));
        var service = CreateService(context);

        var response = await service.GetAsync(SummaryDate);

        Assert.Equal(67m, response.Consumed.Protein);
        Assert.Equal(77m, response.Consumed.Carbohydrates);
        Assert.Equal(11m, response.Consumed.Fat);
        Assert.Equal(675m, response.Consumed.Calories);
    }

    [Fact(DisplayName = "Consumed nutrition scales for amounts other than 100g")]
    public async Task GetAsync_With150GramEntry_ScalesConsumedNutrition()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, "Oats", 7m, 77m, 1m);
        await SeedEntryAsync(context, food.Id, 150m, UtcInsideSummaryDay(12));
        var service = CreateService(context);

        var response = await service.GetAsync(SummaryDate);

        Assert.Equal(10.5m, response.Consumed.Protein);
        Assert.Equal(115.5m, response.Consumed.Carbohydrates);
        Assert.Equal(1.5m, response.Consumed.Fat);
        Assert.Equal(517.5m, response.Consumed.Calories);
    }

    [Fact(DisplayName = "Target nutrition uses current macros and calculated calories")]
    public async Task GetAsync_WithTarget_ReturnsTargetAndCalculatedCalories()
    {
        await using var context = CreateContext();
        await SeedTargetAsync(context, 120m, 250m, 70m);
        var service = CreateService(context);

        var response = await service.GetAsync(SummaryDate);

        Assert.Equal(120m, response.Target.Protein);
        Assert.Equal(250m, response.Target.Carbohydrates);
        Assert.Equal(70m, response.Target.Fat);
        Assert.Equal(2110m, response.Target.Calories);
    }

    [Fact(DisplayName = "Remaining nutrition equals target minus consumed nutrition")]
    public async Task GetAsync_WithEntryAndTarget_SubtractsConsumedFromTarget()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, "Oats", 7m, 77m, 1m);
        await SeedEntryAsync(context, food.Id, 100m, UtcInsideSummaryDay(12));
        await SeedTargetAsync(context, 120m, 250m, 70m);
        var service = CreateService(context);

        var response = await service.GetAsync(SummaryDate);

        Assert.Equal(113m, response.Remaining.Protein);
        Assert.Equal(173m, response.Remaining.Carbohydrates);
        Assert.Equal(69m, response.Remaining.Fat);
        Assert.Equal(1765m, response.Remaining.Calories);
    }

    [Fact(DisplayName = "Exceeding targets produces negative remaining values")]
    public async Task GetAsync_WhenConsumedExceedsTarget_PreservesNegativeRemainingValues()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, "Oats", 7m, 77m, 1m);
        await SeedEntryAsync(context, food.Id, 100m, UtcInsideSummaryDay(12));
        await SeedTargetAsync(context, 5m, 50m, 0m);
        var service = CreateService(context);

        var response = await service.GetAsync(SummaryDate);

        Assert.Equal(-2m, response.Remaining.Protein);
        Assert.Equal(-27m, response.Remaining.Carbohydrates);
        Assert.Equal(-1m, response.Remaining.Fat);
        Assert.Equal(-125m, response.Remaining.Calories);
    }

    [Fact(DisplayName = "An entry exactly at the local day's start is included")]
    public async Task GetAsync_WithEntryAtStartBoundary_IncludesEntry()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, "Oats", 7m, 77m, 1m);
        var start = GetUtcBoundary(SummaryDate);
        await SeedEntryAsync(context, food.Id, 100m, start);
        var service = CreateService(context);

        var response = await service.GetAsync(SummaryDate);

        Assert.Equal(7m, response.Consumed.Protein);
    }

    [Fact(DisplayName = "An entry exactly at the next local day's start is excluded")]
    public async Task GetAsync_WithEntryAtEndBoundary_ExcludesEntry()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, "Oats", 7m, 77m, 1m);
        await SeedEntryAsync(context, food.Id, 100m, GetUtcBoundary(SummaryDate.AddDays(1)));
        var service = CreateService(context);

        var response = await service.GetAsync(SummaryDate);

        Assert.Equal(0m, response.Consumed.Protein);
    }

    [Fact(DisplayName = "UTC timestamps around local midnight map to the correct Bratislava day")]
    public async Task GetAsync_WithUtcTimesAroundLocalMidnight_AssignsCorrectCalendarDay()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, "Oats", 7m, 77m, 1m);
        var localMidnightUtc = GetUtcBoundary(SummaryDate);
        await SeedEntryAsync(context, food.Id, 100m, localMidnightUtc.AddMinutes(-1));
        await SeedEntryAsync(context, food.Id, 150m, localMidnightUtc.AddMinutes(1));
        var service = CreateService(context);

        var response = await service.GetAsync(SummaryDate);

        Assert.Equal(10.5m, response.Consumed.Protein);
        Assert.Equal(115.5m, response.Consumed.Carbohydrates);
        Assert.Equal(1.5m, response.Consumed.Fat);
    }

    private static ProteinTrackerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProteinTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ProteinTrackerDbContext(options);
    }

    private static DailySummaryService CreateService(ProteinTrackerDbContext context)
    {
        return new DailySummaryService(
            new FoodEntryRepository(context),
            new DailyTargetRepository(context),
            BratislavaTimeZone);
    }

    private static DateTimeOffset GetUtcBoundary(DateOnly date)
    {
        var localMidnight = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(localMidnight, BratislavaTimeZone));
    }

    private static DateTimeOffset UtcInsideSummaryDay(int localHour)
    {
        var localTime = SummaryDate.ToDateTime(new TimeOnly(localHour), DateTimeKind.Unspecified);
        return new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(localTime, BratislavaTimeZone));
    }

    private static async Task<Food> SeedFoodAsync(
        ProteinTrackerDbContext context,
        string name,
        decimal protein,
        decimal carbohydrates,
        decimal fat)
    {
        var food = new Food
        {
            Name = name,
            ProteinPer100g = protein,
            CarbohydratesPer100g = carbohydrates,
            FatPer100g = fat
        };

        context.Foods.Add(food);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return food;
    }

    private static async Task SeedEntryAsync(
        ProteinTrackerDbContext context,
        int foodId,
        decimal amount,
        DateTimeOffset consumedAt)
    {
        context.FoodEntries.Add(new FoodEntry
        {
            FoodId = foodId,
            AmountInGrams = amount,
            ConsumedAt = consumedAt
        });

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static async Task SeedTargetAsync(
        ProteinTrackerDbContext context,
        decimal protein,
        decimal carbohydrates,
        decimal fat)
    {
        context.DailyTargets.Add(new DailyTarget
        {
            ProteinTarget = protein,
            CarbohydratesTarget = carbohydrates,
            FatTarget = fat
        });

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }
}
