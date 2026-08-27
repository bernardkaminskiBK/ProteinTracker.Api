namespace ProteinTracker.Api.DTOs;

public class DailySummaryResponse
{
    public DateOnly Date { get; set; }
    public NutritionSummary Consumed { get; set; } = new();
    public NutritionSummary Target { get; set; } = new();
    public NutritionSummary Remaining { get; set; } = new();
}
