namespace ProteinTracker.Api.DTOs;

public class DailyTargetResponse
{
    public decimal ProteinTarget { get; set; }
    public decimal CarbohydratesTarget { get; set; }
    public decimal FatTarget { get; set; }
    public decimal CalorieTarget { get; set; }
}
