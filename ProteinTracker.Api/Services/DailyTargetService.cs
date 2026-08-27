using ProteinTracker.Api.DTOs;
using ProteinTracker.Api.Exceptions;
using ProteinTracker.Api.Models;
using ProteinTracker.Api.Repositories;
using ProteinTracker.Api.Utils;

namespace ProteinTracker.Api.Services;

public class DailyTargetService(DailyTargetRepository dailyTargetRepository)
{
    public async Task<DailyTargetResponse> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var dailyTarget = await dailyTargetRepository.GetCurrentAsync(cancellationToken);

        return dailyTarget is null
            ? new DailyTargetResponse()
            : MapToResponse(dailyTarget);
    }

    public async Task<DailyTargetResponse> UpdateAsync(
        UpdateDailyTargetRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var dailyTarget = await dailyTargetRepository.GetCurrentAsync(cancellationToken);

        if (dailyTarget is null)
        {
            dailyTarget = new DailyTarget
            {
                ProteinTarget = request.ProteinTarget,
                CarbohydratesTarget = request.CarbohydratesTarget,
                FatTarget = request.FatTarget
            };

            await dailyTargetRepository.AddAsync(dailyTarget, cancellationToken);
        }
        else
        {
            dailyTarget.ProteinTarget = request.ProteinTarget;
            dailyTarget.CarbohydratesTarget = request.CarbohydratesTarget;
            dailyTarget.FatTarget = request.FatTarget;

            await dailyTargetRepository.UpdateAsync(dailyTarget, cancellationToken);
        }

        return MapToResponse(dailyTarget);
    }

    private static void Validate(UpdateDailyTargetRequest request)
    {
        if (request.ProteinTarget < 0m)
        {
            throw new BusinessValidationException("Protein target cannot be negative.");
        }

        if (request.CarbohydratesTarget < 0m)
        {
            throw new BusinessValidationException("Carbohydrates target cannot be negative.");
        }

        if (request.FatTarget < 0m)
        {
            throw new BusinessValidationException("Fat target cannot be negative.");
        }
    }

    private static DailyTargetResponse MapToResponse(DailyTarget dailyTarget)
    {
        return new DailyTargetResponse
        {
            ProteinTarget = dailyTarget.ProteinTarget,
            CarbohydratesTarget = dailyTarget.CarbohydratesTarget,
            FatTarget = dailyTarget.FatTarget,
            CalorieTarget = NutritionCalculator.CalculateCalories(
                dailyTarget.ProteinTarget,
                dailyTarget.CarbohydratesTarget,
                dailyTarget.FatTarget)
        };
    }
}
