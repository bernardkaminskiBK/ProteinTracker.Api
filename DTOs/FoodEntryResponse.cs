namespace ProteinTracker.Api.DTOs;

public class FoodEntryResponse
{
    public int Id { get; set; }
    public int FoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public decimal AmountInGrams { get; set; }
    public DateTimeOffset ConsumedAt { get; set; }
    public decimal Protein { get; set; }
    public decimal Carbohydrates { get; set; }
    public decimal Fat { get; set; }
    public decimal Calories { get; set; }
}
