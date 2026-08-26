using ProteinTracker.Api.DTOs;
using ProteinTracker.Api.Exceptions;
using ProteinTracker.Api.Models;
using ProteinTracker.Api.Repositories;
using ProteinTracker.Api.Utils;

namespace ProteinTracker.Api.Services;

public class FoodEntryService(
    FoodEntryRepository foodEntryRepository,
    FoodRepository foodRepository)
{
    public async Task<FoodEntryResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var foodEntry = await GetFoodEntryOrThrowAsync(id, cancellationToken);
        return MapToResponse(foodEntry, foodEntry.Food);
    }

    public async Task<List<FoodEntryResponse>> GetByDateRangeAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken = default)
    {
        var foodEntries = await foodEntryRepository.GetByDateRangeAsync(
            start,
            end,
            cancellationToken);

        return foodEntries
            .Select(foodEntry => MapToResponse(foodEntry, foodEntry.Food))
            .ToList();
    }

    public async Task<FoodEntryResponse> CreateAsync(
        CreateFoodEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateAmount(request.AmountInGrams);
        var food = await GetFoodOrThrowAsync(request.FoodId, cancellationToken);
        EnsureFoodIsActive(food);

        var foodEntry = new FoodEntry
        {
            FoodId = request.FoodId,
            AmountInGrams = request.AmountInGrams,
            ConsumedAt = request.ConsumedAt.ToUniversalTime()
        };

        await foodEntryRepository.AddAsync(foodEntry, cancellationToken);
        return MapToResponse(foodEntry, food);
    }

    public async Task<FoodEntryResponse> UpdateAsync(
        int id,
        UpdateFoodEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateAmount(request.AmountInGrams);
        var foodEntry = await GetFoodEntryOrThrowAsync(id, cancellationToken);
        var responseFood = foodEntry.Food;

        if (request.FoodId != foodEntry.FoodId)
        {
            responseFood = await GetFoodOrThrowAsync(request.FoodId, cancellationToken);
            EnsureFoodIsActive(responseFood);
            foodEntry.FoodId = request.FoodId;
            foodEntry.Food = responseFood;
        }

        foodEntry.AmountInGrams = request.AmountInGrams;
        foodEntry.ConsumedAt = request.ConsumedAt.ToUniversalTime();

        await foodEntryRepository.UpdateAsync(foodEntry, cancellationToken);
        return MapToResponse(foodEntry, responseFood);
    }

    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var foodEntry = await GetFoodEntryOrThrowAsync(id, cancellationToken);
        await foodEntryRepository.DeleteAsync(foodEntry, cancellationToken);
    }

    private async Task<FoodEntry> GetFoodEntryOrThrowAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await foodEntryRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new FoodEntryNotFoundException(id);
    }

    private async Task<Food> GetFoodOrThrowAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await foodRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new FoodNotFoundException(id);
    }

    private static void ValidateAmount(decimal amountInGrams)
    {
        if (amountInGrams <= 0m)
        {
            throw new BusinessValidationException("Amount in grams must be greater than zero.");
        }
    }

    private static void EnsureFoodIsActive(Food food)
    {
        if (food.IsArchived)
        {
            throw new ArchivedFoodException(food.Id);
        }
    }

    private static FoodEntryResponse MapToResponse(FoodEntry foodEntry, Food food)
    {
        var nutrition = NutritionCalculator.CalculateForAmount(
            food.ProteinPer100g,
            food.CarbohydratesPer100g,
            food.FatPer100g,
            foodEntry.AmountInGrams);

        return new FoodEntryResponse
        {
            Id = foodEntry.Id,
            FoodId = foodEntry.FoodId,
            FoodName = food.Name,
            AmountInGrams = foodEntry.AmountInGrams,
            ConsumedAt = foodEntry.ConsumedAt,
            Protein = nutrition.Protein,
            Carbohydrates = nutrition.Carbohydrates,
            Fat = nutrition.Fat,
            Calories = nutrition.Calories
        };
    }
}
