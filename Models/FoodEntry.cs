namespace ProteinTracker.Api.Models;

public class FoodEntry
{
    public int Id { get; set; }

    public int FoodId { get; set; }

    public Food Food { get; set; } = null!;

    public decimal AmountInGrams { get; set; }

    public DateTimeOffset ConsumedAt { get; set; }
}
