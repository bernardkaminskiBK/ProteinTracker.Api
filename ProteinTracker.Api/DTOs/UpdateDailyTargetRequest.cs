namespace ProteinTracker.Api.DTOs;

public class UpdateDailyTargetRequest
{
    public decimal ProteinTarget { get; set; }
    public decimal CarbohydratesTarget { get; set; }
    public decimal FatTarget { get; set; }
}
