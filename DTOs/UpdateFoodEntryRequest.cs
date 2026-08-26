namespace ProteinTracker.Api.DTOs;

public class UpdateFoodEntryRequest
{
    public int FoodId { get; set; }
    public decimal AmountInGrams { get; set; }
    public DateTimeOffset ConsumedAt { get; set; }
}
