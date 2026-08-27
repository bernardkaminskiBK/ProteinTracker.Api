namespace ProteinTracker.Api.Models;

public class FoodEntry
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int FoodId { get; set; }

    public Food Food { get; set; } = null!;

    public decimal AmountInGrams { get; set; }

    public DateTimeOffset ConsumedAt { get; set; }
}
