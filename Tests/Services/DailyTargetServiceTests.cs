using Microsoft.EntityFrameworkCore;
using ProteinTracker.Api.Data;
using ProteinTracker.Api.DTOs;
using ProteinTracker.Api.Exceptions;
using ProteinTracker.Api.Models;
using ProteinTracker.Api.Repositories;
using ProteinTracker.Api.Services;
using Xunit;

namespace ProteinTracker.Api.Tests.Services;

public class DailyTargetServiceTests
{
    [Fact(DisplayName = "GetCurrentAsync returns the existing daily target")]
    public async Task GetCurrentAsync_WithExistingTarget_ReturnsTarget()
    {
        await using var context = CreateContext();
        await SeedTargetAsync(context, 120m, 250m, 70m);
        var service = CreateService(context);

        var response = await service.GetCurrentAsync();

        Assert.Equal(120m, response.ProteinTarget);
        Assert.Equal(250m, response.CarbohydratesTarget);
        Assert.Equal(70m, response.FatTarget);
    }

    [Fact(DisplayName = "GetCurrentAsync returns zeros without persisting when no target exists")]
    public async Task GetCurrentAsync_WithoutTarget_ReturnsZerosAndDoesNotCreateTarget()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var response = await service.GetCurrentAsync();

        Assert.Equal(0m, response.ProteinTarget);
        Assert.Equal(0m, response.CarbohydratesTarget);
        Assert.Equal(0m, response.FatTarget);
        Assert.Equal(0m, response.CalorieTarget);
        Assert.Empty(await context.DailyTargets.ToListAsync());
    }

    [Fact(DisplayName = "UpdateAsync creates the daily target on the first update")]
    public async Task UpdateAsync_WithoutExistingTarget_CreatesTarget()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await service.UpdateAsync(CreateRequest(120m, 250m, 70m));

        var target = await context.DailyTargets.SingleAsync();
        Assert.Equal(120m, target.ProteinTarget);
        Assert.Equal(250m, target.CarbohydratesTarget);
        Assert.Equal(70m, target.FatTarget);
    }

    [Fact(DisplayName = "UpdateAsync modifies the existing target without creating another")]
    public async Task UpdateAsync_WithExistingTarget_UpdatesSingleRecord()
    {
        await using var context = CreateContext();
        var existingTarget = await SeedTargetAsync(context, 100m, 200m, 50m);
        var service = CreateService(context);

        await service.UpdateAsync(CreateRequest(130m, 275m, 80m));

        var targets = await context.DailyTargets.ToListAsync();
        var target = Assert.Single(targets);
        Assert.Equal(existingTarget.Id, target.Id);
        Assert.Equal(130m, target.ProteinTarget);
        Assert.Equal(275m, target.CarbohydratesTarget);
        Assert.Equal(80m, target.FatTarget);
    }

    [Fact(DisplayName = "UpdateAsync rejects a negative protein target")]
    public async Task UpdateAsync_WithNegativeProtein_ThrowsValidationException()
    {
        await AssertInvalidTargetAsync(CreateRequest(-0.1m, 250m, 70m));
    }

    [Fact(DisplayName = "UpdateAsync rejects a negative carbohydrate target")]
    public async Task UpdateAsync_WithNegativeCarbohydrates_ThrowsValidationException()
    {
        await AssertInvalidTargetAsync(CreateRequest(120m, -0.1m, 70m));
    }

    [Fact(DisplayName = "UpdateAsync rejects a negative fat target")]
    public async Task UpdateAsync_WithNegativeFat_ThrowsValidationException()
    {
        await AssertInvalidTargetAsync(CreateRequest(120m, 250m, -0.1m));
    }

    [Fact(DisplayName = "UpdateAsync allows zero target values")]
    public async Task UpdateAsync_WithZeroValues_CreatesZeroTarget()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var response = await service.UpdateAsync(CreateRequest(0m, 0m, 0m));

        Assert.Equal(0m, response.ProteinTarget);
        Assert.Equal(0m, response.CarbohydratesTarget);
        Assert.Equal(0m, response.FatTarget);
        Assert.Equal(0m, response.CalorieTarget);
        Assert.Single(await context.DailyTargets.ToListAsync());
    }

    [Fact(DisplayName = "Daily target response calculates calories from macro targets")]
    public async Task GetCurrentAsync_WithExistingTarget_CalculatesCalorieTarget()
    {
        await using var context = CreateContext();
        await SeedTargetAsync(context, 120m, 250m, 70m);
        var service = CreateService(context);

        var response = await service.GetCurrentAsync();

        Assert.Equal(2110m, response.CalorieTarget);
    }

    private static ProteinTrackerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProteinTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ProteinTrackerDbContext(options);
    }

    private static DailyTargetService CreateService(ProteinTrackerDbContext context)
    {
        return new DailyTargetService(new DailyTargetRepository(context));
    }

    private static UpdateDailyTargetRequest CreateRequest(
        decimal protein,
        decimal carbohydrates,
        decimal fat)
    {
        return new UpdateDailyTargetRequest
        {
            ProteinTarget = protein,
            CarbohydratesTarget = carbohydrates,
            FatTarget = fat
        };
    }

    private static async Task<DailyTarget> SeedTargetAsync(
        ProteinTrackerDbContext context,
        decimal protein,
        decimal carbohydrates,
        decimal fat)
    {
        var target = new DailyTarget
        {
            ProteinTarget = protein,
            CarbohydratesTarget = carbohydrates,
            FatTarget = fat
        };

        context.DailyTargets.Add(target);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return target;
    }

    private static async Task AssertInvalidTargetAsync(UpdateDailyTargetRequest request)
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<BusinessValidationException>(
            () => service.UpdateAsync(request));
        Assert.Empty(await context.DailyTargets.ToListAsync());
    }
}
