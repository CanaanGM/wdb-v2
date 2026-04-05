using Api.Features.Measurements.Contracts;
using Domain.Measurements;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Measurements.Services;

public sealed class MeasurementsService(WorkoutLogDbContext dbContext) : IMeasurementsService
{
    public async Task<List<MeasurementResponse>> GetAllAsync(int userId, CancellationToken cancellationToken)
    {
        return await dbContext.Measurements
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(MapToResponseExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<MeasurementResponse?> GetByIdAsync(int userId, int measurementId, CancellationToken cancellationToken)
    {
        return await dbContext.Measurements
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Id == measurementId)
            .Select(MapToResponseExpression())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<MeasurementOperationResult<MeasurementResponse>> CreateAsync(
        int userId,
        MeasurementUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var entity = new Measurement
        {
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        ApplyRequest(entity, request);

        dbContext.Measurements.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await dbContext.Measurements
            .AsNoTracking()
            .Where(x => x.Id == entity.Id)
            .Select(MapToResponseExpression())
            .SingleAsync(cancellationToken);

        return MeasurementOperationResult<MeasurementResponse>.Success(created);
    }

    public async Task<MeasurementOperationResult<MeasurementResponse>> UpdateAsync(
        int userId,
        int measurementId,
        MeasurementUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Measurements
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Id == measurementId, cancellationToken);
        if (entity is null)
        {
            return MeasurementOperationResult<MeasurementResponse>.NotFound(
                $"Measurement with id '{measurementId}' was not found.");
        }

        ApplyRequest(entity, request);
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await dbContext.Measurements
            .AsNoTracking()
            .Where(x => x.Id == measurementId)
            .Select(MapToResponseExpression())
            .SingleAsync(cancellationToken);

        return MeasurementOperationResult<MeasurementResponse>.Success(updated);
    }

    public async Task<MeasurementOperationResult> DeleteAsync(int userId, int measurementId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Measurements
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Id == measurementId, cancellationToken);
        if (entity is null)
        {
            return MeasurementOperationResult.NotFound($"Measurement with id '{measurementId}' was not found.");
        }

        dbContext.Measurements.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MeasurementOperationResult.Success();
    }

    private static System.Linq.Expressions.Expression<Func<Measurement, MeasurementResponse>> MapToResponseExpression()
    {
        return x => new MeasurementResponse
        {
            Id = x.Id,
            Hip = x.Hip,
            Chest = x.Chest,
            WaistUnderBelly = x.WaistUnderBelly,
            WaistOnBelly = x.WaistOnBelly,
            LeftThigh = x.LeftThigh,
            RightThigh = x.RightThigh,
            LeftCalf = x.LeftCalf,
            RightCalf = x.RightCalf,
            LeftUpperArm = x.LeftUpperArm,
            LeftForearm = x.LeftForearm,
            RightUpperArm = x.RightUpperArm,
            RightForearm = x.RightForearm,
            Neck = x.Neck,
            Minerals = x.Minerals,
            Protein = x.Protein,
            TotalBodyWater = x.TotalBodyWater,
            BodyFatMass = x.BodyFatMass,
            BodyWeight = x.BodyWeight,
            BodyFatPercentage = x.BodyFatPercentage,
            SkeletalMuscleMass = x.SkeletalMuscleMass,
            InBodyScore = x.InBodyScore,
            BodyMassIndex = x.BodyMassIndex,
            BasalMetabolicRate = x.BasalMetabolicRate,
            VisceralFatLevel = x.VisceralFatLevel,
            CreatedAtUtc = x.CreatedAtUtc
        };
    }

    private static void ApplyRequest(Measurement entity, MeasurementUpsertRequest request)
    {
        entity.Hip = request.Hip;
        entity.Chest = request.Chest;
        entity.WaistUnderBelly = request.WaistUnderBelly;
        entity.WaistOnBelly = request.WaistOnBelly;
        entity.LeftThigh = request.LeftThigh;
        entity.RightThigh = request.RightThigh;
        entity.LeftCalf = request.LeftCalf;
        entity.RightCalf = request.RightCalf;
        entity.LeftUpperArm = request.LeftUpperArm;
        entity.LeftForearm = request.LeftForearm;
        entity.RightUpperArm = request.RightUpperArm;
        entity.RightForearm = request.RightForearm;
        entity.Neck = request.Neck;
        entity.Minerals = request.Minerals;
        entity.Protein = request.Protein;
        entity.TotalBodyWater = request.TotalBodyWater;
        entity.BodyFatMass = request.BodyFatMass;
        entity.BodyFatPercentage = request.BodyFatPercentage;
        entity.SkeletalMuscleMass = request.SkeletalMuscleMass;
        entity.InBodyScore = request.InBodyScore;
        entity.BodyMassIndex = request.BodyMassIndex;
        entity.BasalMetabolicRate = request.BasalMetabolicRate;
        entity.VisceralFatLevel = request.VisceralFatLevel;

        var hasComponentsForDerivedBodyWeight = request.Minerals.HasValue
                                                && request.Protein.HasValue
                                                && request.TotalBodyWater.HasValue
                                                && request.BodyFatMass.HasValue;
        entity.BodyWeight = hasComponentsForDerivedBodyWeight
            ? request.Minerals!.Value + request.Protein!.Value + request.TotalBodyWater!.Value + request.BodyFatMass!.Value
            : request.BodyWeight;
    }
}
