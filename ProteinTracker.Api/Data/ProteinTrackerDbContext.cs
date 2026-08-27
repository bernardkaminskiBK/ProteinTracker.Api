using Microsoft.EntityFrameworkCore;
using ProteinTracker.Api.Models;

namespace ProteinTracker.Api.Data;

public class ProteinTrackerDbContext(DbContextOptions<ProteinTrackerDbContext> options)
    : DbContext(options)
{
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<FoodEntry> FoodEntries => Set<FoodEntry>();
    public DbSet<DailyTarget> DailyTargets => Set<DailyTarget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Food>(entity =>
        {
            entity.Property(food => food.ProteinPer100g).HasPrecision(10, 3);
            entity.Property(food => food.CarbohydratesPer100g).HasPrecision(10, 3);
            entity.Property(food => food.FatPer100g).HasPrecision(10, 3);
        });

        modelBuilder.Entity<FoodEntry>(entity =>
        {
            entity.Property(entry => entry.AmountInGrams).HasPrecision(10, 3);

            entity.HasOne(entry => entry.Food)
                .WithMany()
                .HasForeignKey(entry => entry.FoodId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DailyTarget>(entity =>
        {
            entity.Property(target => target.ProteinTarget).HasPrecision(10, 3);
            entity.Property(target => target.CarbohydratesTarget).HasPrecision(10, 3);
            entity.Property(target => target.FatTarget).HasPrecision(10, 3);
        });
    }
}
