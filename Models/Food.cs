namespace ProteinTracker.Api.Models;

public class Food
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal ProteinPer100g { get; set; }

    public decimal CarbohydratesPer100g { get; set; }

    public decimal FatPer100g { get; set; }

    public bool IsArchived { get; set; }
}
