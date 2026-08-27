using ProteinTracker.Api.Utils;
using Xunit;

namespace ProteinTracker.Api.Tests.Utils;

public class NutritionCalculatorTests
{
    [Fact(DisplayName = "CalculateCalories calculates calories from all macros")]
    public void CalculateCalories_WithMacros_ReturnsExpectedCalories()
    {
        var calories = NutritionCalculator.CalculateCalories(10.5m, 20.25m, 3.75m);

        Assert.Equal(156.75m, calories);
    }

    [Fact(DisplayName = "CalculateForAmount returns per-100g nutrition for exactly 100g")]
    public void CalculateForAmount_With100Grams_ReturnsPer100GramValues()
    {
        var result = NutritionCalculator.CalculateForAmount(7m, 77m, 1m, 100m);

        Assert.Equal(7m, result.Protein);
        Assert.Equal(77m, result.Carbohydrates);
        Assert.Equal(1m, result.Fat);
        Assert.Equal(345m, result.Calories);
    }

    [Fact(DisplayName = "CalculateForAmount scales nutrition to 150g")]
    public void CalculateForAmount_With150Grams_ReturnsOneAndAHalfTimesValues()
    {
        var result = NutritionCalculator.CalculateForAmount(7m, 77m, 1m, 150m);

        Assert.Equal(10.5m, result.Protein);
        Assert.Equal(115.5m, result.Carbohydrates);
        Assert.Equal(1.5m, result.Fat);
        Assert.Equal(517.5m, result.Calories);
    }

    [Fact(DisplayName = "CalculateForAmount scales nutrition to 50g")]
    public void CalculateForAmount_With50Grams_ReturnsHalfValues()
    {
        var result = NutritionCalculator.CalculateForAmount(7m, 77m, 1m, 50m);

        Assert.Equal(3.5m, result.Protein);
        Assert.Equal(38.5m, result.Carbohydrates);
        Assert.Equal(0.5m, result.Fat);
        Assert.Equal(172.5m, result.Calories);
    }

    [Fact(DisplayName = "CalculateCalories returns zero when all macros are zero")]
    public void CalculateCalories_WithZeroMacros_ReturnsZero()
    {
        var calories = NutritionCalculator.CalculateCalories(0m, 0m, 0m);

        Assert.Equal(0m, calories);
    }

    [Fact(DisplayName = "CalculateForAmount preserves fractional decimal values")]
    public void CalculateForAmount_WithFractionalValues_PreservesPrecision()
    {
        var result = NutritionCalculator.CalculateForAmount(
            1.25m,
            23.75m,
            0.625m,
            12.5m);

        Assert.Equal(0.15625m, result.Protein);
        Assert.Equal(2.96875m, result.Carbohydrates);
        Assert.Equal(0.078125m, result.Fat);
        Assert.Equal(13.203125m, result.Calories);
    }
}
