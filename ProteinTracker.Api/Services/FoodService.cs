using ProteinTracker.Api.DTOs;
using ProteinTracker.Api.Exceptions;
using ProteinTracker.Api.Models;
using ProteinTracker.Api.Repositories;
using ProteinTracker.Api.Utils;

namespace ProteinTracker.Api.Services;

public class FoodService(FoodRepository foodRepository)
{
    public async Task<List<FoodResponse>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var foods = await foodRepository.GetAllActiveAsync(cancellationToken);
        return foods.Select(MapToResponse).ToList();
    }

    public async Task<List<FoodResponse>> GetAllArchivedAsync(
        CancellationToken cancellationToken = default)
    {
        var foods = await foodRepository.GetAllArchivedAsync(cancellationToken);
        return foods.Select(MapToResponse).ToList();
    }

    public async Task<FoodResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var food = await GetFoodOrThrowAsync(id, cancellationToken);
        return MapToResponse(food);
    }

    public async Task<FoodResponse> CreateAsync(
        CreateFoodRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.ProteinPer100g, request.CarbohydratesPer100g, request.FatPer100g);

        var food = new Food
        {
            Name = request.Name.Trim(),
            ProteinPer100g = request.ProteinPer100g,
            CarbohydratesPer100g = request.CarbohydratesPer100g,
            FatPer100g = request.FatPer100g,
            IsArchived = false
        };

        await foodRepository.AddAsync(food, cancellationToken);
        return MapToResponse(food);
    }

    public async Task<FoodResponse> UpdateAsync(
        int id,
        UpdateFoodRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.ProteinPer100g, request.CarbohydratesPer100g, request.FatPer100g);

        var food = await GetFoodOrThrowAsync(id, cancellationToken);
        food.Name = request.Name.Trim();
        food.ProteinPer100g = request.ProteinPer100g;
        food.CarbohydratesPer100g = request.CarbohydratesPer100g;
        food.FatPer100g = request.FatPer100g;

        await foodRepository.UpdateAsync(food, cancellationToken);
        return MapToResponse(food);
    }

    public async Task<FoodResponse> ArchiveAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var food = await GetFoodOrThrowAsync(id, cancellationToken);

        if (!food.IsArchived)
        {
            food.IsArchived = true;
            await foodRepository.UpdateAsync(food, cancellationToken);
        }

        return MapToResponse(food);
    }

    public async Task<FoodResponse> RestoreAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var food = await GetFoodOrThrowAsync(id, cancellationToken);

        if (food.IsArchived)
        {
            food.IsArchived = false;
            await foodRepository.UpdateAsync(food, cancellationToken);
        }

        return MapToResponse(food);
    }

    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var food = await GetFoodOrThrowAsync(id, cancellationToken);

        if (!food.IsArchived)
        {
            throw new BusinessValidationException("Only archived foods can be permanently deleted.");
        }

        if (await foodRepository.HasFoodEntriesAsync(id, cancellationToken))
        {
            throw new FoodDeletionConflictException(id);
        }

        await foodRepository.DeleteAsync(food, cancellationToken);
    }

    private async Task<Food> GetFoodOrThrowAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await foodRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new FoodNotFoundException(id);
    }

    private static void Validate(
        string? name,
        decimal proteinPer100g,
        decimal carbohydratesPer100g,
        decimal fatPer100g)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessValidationException("Food name is required.");
        }

        if (proteinPer100g < 0m)
        {
            throw new BusinessValidationException("Protein per 100g cannot be negative.");
        }

        if (carbohydratesPer100g < 0m)
        {
            throw new BusinessValidationException("Carbohydrates per 100g cannot be negative.");
        }

        if (fatPer100g < 0m)
        {
            throw new BusinessValidationException("Fat per 100g cannot be negative.");
        }
    }

    private static FoodResponse MapToResponse(Food food)
    {
        return new FoodResponse
        {
            Id = food.Id,
            Name = food.Name,
            ProteinPer100g = food.ProteinPer100g,
            CarbohydratesPer100g = food.CarbohydratesPer100g,
            FatPer100g = food.FatPer100g,
            CaloriesPer100g = NutritionCalculator.CalculateCalories(
                food.ProteinPer100g,
                food.CarbohydratesPer100g,
                food.FatPer100g),
            IsArchived = food.IsArchived
        };
    }
}
