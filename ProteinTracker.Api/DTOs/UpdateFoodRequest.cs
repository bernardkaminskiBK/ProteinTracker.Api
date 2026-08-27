namespace ProteinTracker.Api.DTOs;

public class UpdateFoodRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal ProteinPer100g { get; set; }
    public decimal CarbohydratesPer100g { get; set; }
    public decimal FatPer100g { get; set; }
}
