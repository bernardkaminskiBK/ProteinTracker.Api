namespace ProteinTracker.Api.Models;

public class DailyTarget
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public decimal ProteinTarget { get; set; }

    public decimal CarbohydratesTarget { get; set; }

    public decimal FatTarget { get; set; }
}
