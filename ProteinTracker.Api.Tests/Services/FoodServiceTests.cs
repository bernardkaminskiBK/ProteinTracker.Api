using Microsoft.EntityFrameworkCore;
using ProteinTracker.Api.Data;
using ProteinTracker.Api.DTOs;
using ProteinTracker.Api.Exceptions;
using ProteinTracker.Api.Models;
using ProteinTracker.Api.Repositories;
using ProteinTracker.Api.Services;
using Xunit;

namespace ProteinTracker.Api.Tests.Services;

public class FoodServiceTests
{
    [Fact(DisplayName = "CreateAsync creates a valid active food")]
    public async Task CreateAsync_WithValidRequest_CreatesActiveFood()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var response = await service.CreateAsync(CreateRequest());

        var storedFood = await context.Foods.SingleAsync();
        Assert.Equal(response.Id, storedFood.Id);
        Assert.Equal("Oats", storedFood.Name);
        Assert.False(
            storedFood.IsArchived,
            "New foods must begin active so they can be selected for food entries.");
    }

    [Fact(DisplayName = "CreateAsync trims leading and trailing whitespace from the name")]
    public async Task CreateAsync_WithPaddedName_StoresTrimmedName()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var request = CreateRequest();
        request.Name = "  Oats  ";

        var response = await service.CreateAsync(request);

        Assert.Equal("Oats", response.Name);
        Assert.Equal("Oats", (await context.Foods.SingleAsync()).Name);
    }

    [Theory(DisplayName = "CreateAsync rejects an empty or whitespace name")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithBlankName_ThrowsValidationException(string name)
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var request = CreateRequest();
        request.Name = name;

        await Assert.ThrowsAsync<BusinessValidationException>(
            () => service.CreateAsync(request));
    }

    [Fact(DisplayName = "CreateAsync rejects negative protein")]
    public async Task CreateAsync_WithNegativeProtein_ThrowsValidationException()
    {
        await AssertInvalidMacrosAsync(protein: -0.1m, carbohydrates: 77m, fat: 1m);
    }

    [Fact(DisplayName = "CreateAsync rejects negative carbohydrates")]
    public async Task CreateAsync_WithNegativeCarbohydrates_ThrowsValidationException()
    {
        await AssertInvalidMacrosAsync(protein: 7m, carbohydrates: -0.1m, fat: 1m);
    }

    [Fact(DisplayName = "CreateAsync rejects negative fat")]
    public async Task CreateAsync_WithNegativeFat_ThrowsValidationException()
    {
        await AssertInvalidMacrosAsync(protein: 7m, carbohydrates: 77m, fat: -0.1m);
    }

    [Fact(DisplayName = "GetByIdAsync throws FoodNotFoundException for a missing food")]
    public async Task GetByIdAsync_WithMissingId_ThrowsFoodNotFoundException()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<FoodNotFoundException>(
            () => service.GetByIdAsync(404));
    }

    [Fact(DisplayName = "UpdateAsync throws FoodNotFoundException for a missing food")]
    public async Task UpdateAsync_WithMissingId_ThrowsFoodNotFoundException()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<FoodNotFoundException>(
            () => service.UpdateAsync(404, new UpdateFoodRequest
            {
                Name = "Oats",
                ProteinPer100g = 7m,
                CarbohydratesPer100g = 77m,
                FatPer100g = 1m
            }));
    }

    [Fact(DisplayName = "ArchiveAsync changes IsArchived to true")]
    public async Task ArchiveAsync_WithActiveFood_ArchivesFood()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, isArchived: false);
        var service = CreateService(context);

        var response = await service.ArchiveAsync(food.Id);

        Assert.True(
            response.IsArchived,
            "Archiving a food must be reflected in the service response.");
        Assert.True(
            (await context.Foods.SingleAsync()).IsArchived,
            "Archiving a food must persist the soft-delete state.");
    }

    [Fact(DisplayName = "ArchiveAsync is idempotent for an archived food")]
    public async Task ArchiveAsync_WithArchivedFood_RemainsArchived()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, isArchived: true);
        var service = CreateService(context);

        var firstResponse = await service.ArchiveAsync(food.Id);
        var secondResponse = await service.ArchiveAsync(food.Id);

        Assert.True(
            firstResponse.IsArchived,
            "An already archived food must remain archived.");
        Assert.True(
            secondResponse.IsArchived,
            "Repeated archive operations must succeed without changing the archived state.");
        Assert.True(
            (await context.Foods.SingleAsync()).IsArchived,
            "Idempotent archive operations must preserve the persisted archived state.");
    }

    [Fact(DisplayName = "RestoreAsync changes IsArchived to false")]
    public async Task RestoreAsync_WithArchivedFood_RestoresFood()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, isArchived: true);
        var service = CreateService(context);

        var response = await service.RestoreAsync(food.Id);

        Assert.False(
            response.IsArchived,
            "Restoring a food must be reflected in the service response.");
        Assert.False(
            (await context.Foods.SingleAsync()).IsArchived,
            "Restoring a food must persist the active state.");
    }

    [Fact(DisplayName = "RestoreAsync is idempotent for an active food")]
    public async Task RestoreAsync_WithActiveFood_RemainsActive()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, isArchived: false);
        var service = CreateService(context);

        var firstResponse = await service.RestoreAsync(food.Id);
        var secondResponse = await service.RestoreAsync(food.Id);

        Assert.False(
            firstResponse.IsArchived,
            "An already active food must remain active.");
        Assert.False(
            secondResponse.IsArchived,
            "Repeated restore operations must succeed without changing the active state.");
        Assert.False(
            (await context.Foods.SingleAsync()).IsArchived,
            "Idempotent restore operations must preserve the persisted active state.");
    }

    [Fact(DisplayName = "Food responses calculate CaloriesPer100g from macros")]
    public async Task GetByIdAsync_WithExistingFood_CalculatesCaloriesPer100g()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, isArchived: false);
        var service = CreateService(context);

        var response = await service.GetByIdAsync(food.Id);

        Assert.Equal(345m, response.CaloriesPer100g);
    }

    [Fact(DisplayName = "DeleteAsync permanently deletes an unused archived food")]
    public async Task DeleteAsync_WithUnusedArchivedFood_DeletesFood()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, isArchived: true);
        var service = CreateService(context);

        await service.DeleteAsync(food.Id);

        Assert.False(
            await context.Foods.AnyAsync(),
            "An archived food without historical references may be permanently deleted.");
    }

    [Fact(DisplayName = "DeleteAsync rejects an active food")]
    public async Task DeleteAsync_WithActiveFood_ThrowsValidationException()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, isArchived: false);
        var service = CreateService(context);

        await Assert.ThrowsAsync<BusinessValidationException>(
            () => service.DeleteAsync(food.Id));

        Assert.True(
            await context.Foods.AnyAsync(item => item.Id == food.Id),
            "Active foods must be archived before permanent deletion is considered.");
    }

    [Fact(DisplayName = "DeleteAsync rejects an archived food referenced by historical entries")]
    public async Task DeleteAsync_WithReferencedArchivedFood_ThrowsConflictException()
    {
        await using var context = CreateContext();
        var food = await SeedFoodAsync(context, isArchived: true);
        context.FoodEntries.Add(new FoodEntry
        {
            UserId = 1,
            FoodId = food.Id,
            AmountInGrams = 100m,
            ConsumedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = CreateService(context);

        await Assert.ThrowsAsync<FoodDeletionConflictException>(
            () => service.DeleteAsync(food.Id));

        Assert.True(
            await context.Foods.AnyAsync(item => item.Id == food.Id),
            "Foods referenced by historical entries must remain available for recalculation.");
        Assert.True(
            await context.FoodEntries.CountAsync() == 1,
            "Rejecting food deletion must not delete or modify historical entries.");
    }

    [Fact(DisplayName = "DeleteAsync throws FoodNotFoundException for a missing food")]
    public async Task DeleteAsync_WithMissingId_ThrowsFoodNotFoundException()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<FoodNotFoundException>(
            () => service.DeleteAsync(404));
    }

    private static ProteinTrackerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProteinTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ProteinTrackerDbContext(options);
    }

    private static FoodService CreateService(ProteinTrackerDbContext context)
    {
        return new FoodService(new FoodRepository(context), new ProteinTracker.Api.Security.CurrentUser(1));
    }

    private static CreateFoodRequest CreateRequest()
    {
        return new CreateFoodRequest
        {
            Name = "Oats",
            ProteinPer100g = 7m,
            CarbohydratesPer100g = 77m,
            FatPer100g = 1m
        };
    }

    private static async Task<Food> SeedFoodAsync(
        ProteinTrackerDbContext context,
        bool isArchived)
    {
        var food = new Food
        {
            UserId = 1,
            Name = "Oats",
            ProteinPer100g = 7m,
            CarbohydratesPer100g = 77m,
            FatPer100g = 1m,
            IsArchived = isArchived
        };

        context.Foods.Add(food);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return food;
    }

    private static async Task AssertInvalidMacrosAsync(
        decimal protein,
        decimal carbohydrates,
        decimal fat)
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var request = CreateRequest();
        request.ProteinPer100g = protein;
        request.CarbohydratesPer100g = carbohydrates;
        request.FatPer100g = fat;

        await Assert.ThrowsAsync<BusinessValidationException>(
            () => service.CreateAsync(request));
    }
}
