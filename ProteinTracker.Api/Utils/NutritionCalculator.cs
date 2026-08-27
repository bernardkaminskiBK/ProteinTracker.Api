using ProteinTracker.Api.DTOs;

namespace ProteinTracker.Api.Utils;

public static class NutritionCalculator
{
    public static decimal CalculateCalories(
        decimal protein,
        decimal carbohydrates,
        decimal fat)
    {
        return protein * 4m + carbohydrates * 4m + fat * 9m;
    }

    public static NutritionSummary CalculateForAmount(
        decimal proteinPer100g,
        decimal carbohydratesPer100g,
        decimal fatPer100g,
        decimal amountInGrams)
    {
        var protein = proteinPer100g * amountInGrams / 100m;
        var carbohydrates = carbohydratesPer100g * amountInGrams / 100m;
        var fat = fatPer100g * amountInGrams / 100m;

        return new NutritionSummary
        {
            Protein = protein,
            Carbohydrates = carbohydrates,
            Fat = fat,
            Calories = CalculateCalories(protein, carbohydrates, fat)
        };
    }
}
