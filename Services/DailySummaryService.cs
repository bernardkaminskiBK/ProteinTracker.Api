using ProteinTracker.Api.DTOs;
using ProteinTracker.Api.Repositories;
using ProteinTracker.Api.Utils;

namespace ProteinTracker.Api.Services;

public class DailySummaryService(
    FoodEntryRepository foodEntryRepository,
    DailyTargetRepository dailyTargetRepository,
    TimeZoneInfo localTimeZone)
{
    public async Task<DailySummaryResponse> GetAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localEnd = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var start = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(localStart, localTimeZone));
        var end = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(localEnd, localTimeZone));

        var foodEntries = await foodEntryRepository.GetByDateRangeAsync(
            start,
            end,
            cancellationToken);
        var dailyTarget = await dailyTargetRepository.GetCurrentAsync(cancellationToken);

        var consumed = new NutritionSummary();

        foreach (var foodEntry in foodEntries)
        {
            var nutrition = NutritionCalculator.CalculateForAmount(
                foodEntry.Food.ProteinPer100g,
                foodEntry.Food.CarbohydratesPer100g,
                foodEntry.Food.FatPer100g,
                foodEntry.AmountInGrams);

            consumed.Protein += nutrition.Protein;
            consumed.Carbohydrates += nutrition.Carbohydrates;
            consumed.Fat += nutrition.Fat;
            consumed.Calories += nutrition.Calories;
        }

        var target = dailyTarget is null
            ? new NutritionSummary()
            : new NutritionSummary
            {
                Protein = dailyTarget.ProteinTarget,
                Carbohydrates = dailyTarget.CarbohydratesTarget,
                Fat = dailyTarget.FatTarget,
                Calories = NutritionCalculator.CalculateCalories(
                    dailyTarget.ProteinTarget,
                    dailyTarget.CarbohydratesTarget,
                    dailyTarget.FatTarget)
            };

        return new DailySummaryResponse
        {
            Date = date,
            Consumed = consumed,
            Target = target,
            Remaining = new NutritionSummary
            {
                Protein = target.Protein - consumed.Protein,
                Carbohydrates = target.Carbohydrates - consumed.Carbohydrates,
                Fat = target.Fat - consumed.Fat,
                Calories = target.Calories - consumed.Calories
            }
        };
    }
}
