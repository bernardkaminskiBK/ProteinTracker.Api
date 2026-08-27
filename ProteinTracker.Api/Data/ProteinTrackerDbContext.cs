using Microsoft.EntityFrameworkCore;
using ProteinTracker.Api.Models;

namespace ProteinTracker.Api.Data;

public class ProteinTrackerDbContext(DbContextOptions<ProteinTrackerDbContext> options)
    : DbContext(options)
{
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<FoodEntry> FoodEntries => Set<FoodEntry>();
    public DbSet<DailyTarget> DailyTargets => Set<DailyTarget>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(user => user.Email).HasMaxLength(320);
            entity.Property(user => user.NormalizedEmail).HasMaxLength(320);
            entity.Property(user => user.PasswordHash).HasMaxLength(1024);
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
        });

        modelBuilder.Entity<Food>(entity =>
        {
            entity.Property(food => food.ProteinPer100g).HasPrecision(10, 3);
            entity.Property(food => food.CarbohydratesPer100g).HasPrecision(10, 3);
            entity.Property(food => food.FatPer100g).HasPrecision(10, 3);
            entity.HasAlternateKey(food => new { food.Id, food.UserId });
            entity.HasOne(food => food.User)
                .WithMany()
                .HasForeignKey(food => food.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FoodEntry>(entity =>
        {
            entity.Property(entry => entry.AmountInGrams).HasPrecision(10, 3);

            entity.HasOne(entry => entry.User)
                .WithMany()
                .HasForeignKey(entry => entry.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(entry => entry.Food)
                .WithMany()
                .HasForeignKey(entry => new { entry.FoodId, entry.UserId })
                .HasPrincipalKey(food => new { food.Id, food.UserId })
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DailyTarget>(entity =>
        {
            entity.Property(target => target.ProteinTarget).HasPrecision(10, 3);
            entity.Property(target => target.CarbohydratesTarget).HasPrecision(10, 3);
            entity.Property(target => target.FatTarget).HasPrecision(10, 3);
            entity.HasIndex(target => target.UserId).IsUnique();
            entity.HasOne(target => target.User)
                .WithMany()
                .HasForeignKey(target => target.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
