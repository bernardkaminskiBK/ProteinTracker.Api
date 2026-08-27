using Microsoft.EntityFrameworkCore;
using ProteinTracker.Api.Data;
using ProteinTracker.Api.DTOs;
using ProteinTracker.Api.Exceptions;
using ProteinTracker.Api.Models;
using ProteinTracker.Api.Repositories;
using ProteinTracker.Api.Services;
using Xunit;

namespace ProteinTracker.Api.Tests.Services;

public class FoodEntryServiceTests
{
    [Fact(DisplayName = "CreateAsync creates a valid food entry")]
    public async Task CreateAsync_WithValidRequest_CreatesFoodEntry()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context);
        var service = CreateService(context);
        var consumedAt = new DateTimeOffset(2026, 8, 26, 12, 30, 0, TimeSpan.Zero);

        var response = await service.CreateAsync(new CreateFoodEntryRequest
        {
            FoodId = food.Id,
            AmountInGrams = 150m,
            ConsumedAt = consumedAt
        });

        var storedEntry = await context.FoodEntries.SingleAsync();
        Assert.Equal(response.Id, storedEntry.Id);
        Assert.Equal(food.Id, storedEntry.FoodId);
        Assert.Equal(150m, storedEntry.AmountInGrams);
        Assert.Equal(consumedAt, storedEntry.ConsumedAt);
    }

    [Fact(DisplayName = "CreateAsync normalizes an offset-aware timestamp to UTC")]
    public async Task CreateAsync_WithNonZeroOffset_StoresEquivalentUtcTimestamp()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context);
        var service = CreateService(context);
        var submittedAt = new DateTimeOffset(2026, 8, 26, 12, 30, 0, TimeSpan.FromHours(2));

        await service.CreateAsync(new CreateFoodEntryRequest
        {
            FoodId = food.Id,
            AmountInGrams = 150m,
            ConsumedAt = submittedAt
        });

        var storedAt = (await context.FoodEntries.SingleAsync()).ConsumedAt;
        Assert.Equal(submittedAt.ToUniversalTime(), storedAt);
        Assert.Equal(TimeSpan.Zero, storedAt.Offset);
    }

    [Theory(DisplayName = "CreateAsync rejects a non-positive amount")]
    [InlineData("0")]
    [InlineData("-0.1")]
    public async Task CreateAsync_WithNonPositiveAmount_ThrowsValidationException(
        string amountText)
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context);
        var service = CreateService(context);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.CreateAsync(new CreateFoodEntryRequest
            {
                FoodId = food.Id,
                AmountInGrams = decimal.Parse(amountText),
                ConsumedAt = DateTimeOffset.UtcNow
            }));
    }

    [Fact(DisplayName = "CreateAsync throws FoodNotFoundException for a missing food")]
    public async Task CreateAsync_WithMissingFood_ThrowsFoodNotFoundException()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<FoodNotFoundException>(() =>
            service.CreateAsync(new CreateFoodEntryRequest
            {
                FoodId = 404,
                AmountInGrams = 100m,
                ConsumedAt = DateTimeOffset.UtcNow
            }));
    }

    [Fact(DisplayName = "CreateAsync rejects an archived food")]
    public async Task CreateAsync_WithArchivedFood_ThrowsArchivedFoodException()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, isArchived: true);
        var service = CreateService(context);

        await Assert.ThrowsAsync<ArchivedFoodException>(() =>
            service.CreateAsync(new CreateFoodEntryRequest
            {
                FoodId = food.Id,
                AmountInGrams = 100m,
                ConsumedAt = DateTimeOffset.UtcNow
            }));
    }

    [Fact(DisplayName = "GetByIdAsync throws FoodEntryNotFoundException for a missing entry")]
    public async Task GetByIdAsync_WithMissingEntry_ThrowsFoodEntryNotFoundException()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<FoodEntryNotFoundException>(
            () => service.GetByIdAsync(404));
    }

    [Fact(DisplayName = "Food entry response calculates nutrition for the consumed amount")]
    public async Task GetByIdAsync_With150Grams_CalculatesNutrition()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context);
        var entry = await SeedEntryAsync(context, food.Id, 150m, DateTimeOffset.UtcNow);
        var service = CreateService(context);

        var response = await service.GetByIdAsync(entry.Id);

        Assert.Equal(10.5m, response.Protein);
        Assert.Equal(115.5m, response.Carbohydrates);
        Assert.Equal(1.5m, response.Fat);
        Assert.Equal(517.5m, response.Calories);
    }

    [Fact(DisplayName = "UpdateAsync updates amount and consumed timestamp")]
    public async Task UpdateAsync_WithNewAmountAndDate_UpdatesEntry()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context);
        var entry = await SeedEntryAsync(context, food.Id, 100m, DateTimeOffset.UtcNow);
        var service = CreateService(context);
        var newDate = new DateTimeOffset(2026, 9, 1, 18, 15, 0, TimeSpan.Zero);

        var response = await service.UpdateAsync(entry.Id, new UpdateFoodEntryRequest
        {
            FoodId = food.Id,
            AmountInGrams = 175m,
            ConsumedAt = newDate
        });

        Assert.Equal(175m, response.AmountInGrams);
        Assert.Equal(newDate, response.ConsumedAt);
        var storedEntry = await context.FoodEntries.SingleAsync();
        Assert.Equal(175m, storedEntry.AmountInGrams);
        Assert.Equal(newDate, storedEntry.ConsumedAt);
    }

    [Fact(DisplayName = "UpdateAsync normalizes an offset-aware timestamp to UTC")]
    public async Task UpdateAsync_WithNonZeroOffset_StoresEquivalentUtcTimestamp()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context);
        var entry = await SeedEntryAsync(context, food.Id, 100m, DateTimeOffset.UtcNow);
        var service = CreateService(context);
        var submittedAt = new DateTimeOffset(2026, 8, 26, 18, 15, 0, TimeSpan.FromHours(-4));

        await service.UpdateAsync(entry.Id, new UpdateFoodEntryRequest
        {
            FoodId = food.Id,
            AmountInGrams = 100m,
            ConsumedAt = submittedAt
        });

        var storedAt = (await context.FoodEntries.SingleAsync()).ConsumedAt;
        Assert.Equal(submittedAt.ToUniversalTime(), storedAt);
        Assert.Equal(TimeSpan.Zero, storedAt.Offset);
    }

    [Fact(DisplayName = "UpdateAsync reassigns an entry to another active food")]
    public async Task UpdateAsync_WithAnotherActiveFood_ChangesFood()
    {
        await using var context = CreateContext();
        var originalFood = await SeedFoodAsync(context);
        var newFood = await SeedFoodAsync(context, name: "Rice", protein: 3m, carbohydrates: 28m, fat: 0.3m);
        var entry = await SeedEntryAsync(context, originalFood.Id, 100m, DateTimeOffset.UtcNow);
        var service = CreateService(context);

        var response = await service.UpdateAsync(entry.Id, new UpdateFoodEntryRequest
        {
            FoodId = newFood.Id,
            AmountInGrams = 100m,
            ConsumedAt = entry.ConsumedAt
        });

        Assert.Equal(newFood.Id, response.FoodId);
        Assert.Equal("Rice", response.FoodName);
        Assert.Equal(newFood.Id, (await context.FoodEntries.SingleAsync()).FoodId);
    }

    [Fact(DisplayName = "UpdateAsync rejects reassignment to an archived food")]
    public async Task UpdateAsync_WithArchivedNewFood_ThrowsArchivedFoodException()
    {
        await using var context = CreateContext();
        var originalFood = await SeedFoodAsync(context);
        var archivedFood = await SeedFoodAsync(context, isArchived: true, name: "Old food");
        var entry = await SeedEntryAsync(context, originalFood.Id, 100m, DateTimeOffset.UtcNow);
        var service = CreateService(context);

        await Assert.ThrowsAsync<ArchivedFoodException>(() =>
            service.UpdateAsync(entry.Id, new UpdateFoodEntryRequest
            {
                FoodId = archivedFood.Id,
                AmountInGrams = 100m,
                ConsumedAt = entry.ConsumedAt
            }));
    }

    [Fact(DisplayName = "UpdateAsync allows amount and date corrections for an archived existing food")]
    public async Task UpdateAsync_WhenExistingFoodWasArchived_AllowsOtherChanges()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, isArchived: true);
        var entry = await SeedEntryAsync(context, food.Id, 100m, DateTimeOffset.UtcNow);
        var service = CreateService(context);
        var correctedDate = entry.ConsumedAt.AddHours(-2);

        var response = await service.UpdateAsync(entry.Id, new UpdateFoodEntryRequest
        {
            FoodId = food.Id,
            AmountInGrams = 125m,
            ConsumedAt = correctedDate
        });

        Assert.Equal(food.Id, response.FoodId);
        Assert.Equal(125m, response.AmountInGrams);
        Assert.Equal(correctedDate, response.ConsumedAt);
    }

    [Fact(DisplayName = "DeleteAsync physically deletes an existing food entry")]
    public async Task DeleteAsync_WithExistingEntry_DeletesEntry()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context);
        var entry = await SeedEntryAsync(context, food.Id, 100m, DateTimeOffset.UtcNow);
        var service = CreateService(context);

        await service.DeleteAsync(entry.Id);

        Assert.Empty(await context.FoodEntries.ToListAsync());
    }

    [Fact(DisplayName = "DeleteAsync throws FoodEntryNotFoundException for a missing entry")]
    public async Task DeleteAsync_WithMissingEntry_ThrowsFoodEntryNotFoundException()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<FoodEntryNotFoundException>(
            () => service.DeleteAsync(404));
    }

    [Fact(DisplayName = "GetByDateRangeAsync returns only entries in the supplied half-open range")]
    public async Task GetByDateRangeAsync_WithRange_ReturnsMatchingEntries()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context);
        var start = new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddDays(1);
        await SeedEntryAsync(context, food.Id, 50m, start.AddTicks(-1));
        var atStart = await SeedEntryAsync(context, food.Id, 100m, start);
        var inside = await SeedEntryAsync(context, food.Id, 150m, start.AddHours(12));
        await SeedEntryAsync(context, food.Id, 200m, end);
        var service = CreateService(context);

        var responses = await service.GetByDateRangeAsync(start, end);

        Assert.Equal(2, responses.Count);
        Assert.Contains(responses, response => response.Id == atStart.Id);
        Assert.Contains(responses, response => response.Id == inside.Id);
    }

    [Fact(DisplayName = "GetByDateRangeAsync normalizes offset-aware boundaries to the equivalent UTC range")]
    public async Task GetByDateRangeAsync_WithNonZeroOffsets_QueriesEquivalentUtcRange()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context);
        var start = new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.FromHours(2));
        var end = new DateTimeOffset(2026, 8, 27, 0, 0, 0, TimeSpan.FromHours(2));
        await SeedEntryAsync(context, food.Id, 50m, start.ToUniversalTime().AddTicks(-1));
        var atStart = await SeedEntryAsync(context, food.Id, 100m, start.ToUniversalTime());
        var inside = await SeedEntryAsync(context, food.Id, 150m, end.ToUniversalTime().AddTicks(-1));
        await SeedEntryAsync(context, food.Id, 200m, end.ToUniversalTime());
        var service = CreateService(context);

        var responses = await service.GetByDateRangeAsync(start, end);

        Assert.Equal(2, responses.Count);
        Assert.Contains(responses, response => response.Id == atStart.Id);
        Assert.Contains(responses, response => response.Id == inside.Id);
    }

    private static ProteinTrackerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProteinTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ProteinTrackerDbContext(options);
    }

    private static FoodEntryService CreateService(ProteinTrackerDbContext context)
    {
        return new FoodEntryService(
            new FoodEntryRepository(context),
            new FoodRepository(context));
    }

    private static async Task<Food> SeedFoodAsync(
        ProteinTrackerDbContext context,
        bool isArchived = false,
        string name = "Oats",
        decimal protein = 7m,
        decimal carbohydrates = 77m,
        decimal fat = 1m)
    {
        var food = new Food
        {
            Name = name,
            ProteinPer100g = protein,
            CarbohydratesPer100g = carbohydrates,
            FatPer100g = fat,
            IsArchived = isArchived
        };

        context.Foods.Add(food);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return food;
    }

    private static async Task<FoodEntry> SeedEntryAsync(
        ProteinTrackerDbContext context,
        int foodId,
        decimal amount,
        DateTimeOffset consumedAt)
    {
        var foodEntry = new FoodEntry
        {
            FoodId = foodId,
            AmountInGrams = amount,
            ConsumedAt = consumedAt
        };

        context.FoodEntries.Add(foodEntry);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return foodEntry;
    }
}
