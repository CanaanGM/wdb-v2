using Api.Application.Text;
using Api.Features.TrainingTypes.Contracts;
using Domain.TrainingTypes;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.TrainingTypes.Services;

public sealed class TrainingTypesService(WorkoutLogDbContext dbContext) : ITrainingTypesService
{
    public async Task<List<TrainingTypeResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.TrainingTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new TrainingTypeResponse
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TrainingTypeOperationResult<TrainingTypeResponse>> CreateAsync(
        CreateTrainingTypeRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TrainingTypeOperationResult<TrainingTypeResponse>.ValidationError("Name is required.");
        }

        var normalizedName = StorageTextNormalizer.NormalizeKey(request.Name);

        var exists = await dbContext.TrainingTypes
            .AsNoTracking()
            .AnyAsync(x => x.Name == normalizedName, cancellationToken);
        if (exists)
        {
            return TrainingTypeOperationResult<TrainingTypeResponse>.Conflict(
                $"Training type '{normalizedName}' already exists.");
        }

        var entity = new TrainingType
        {
            Name = normalizedName
        };

        dbContext.TrainingTypes.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return TrainingTypeOperationResult<TrainingTypeResponse>.Conflict(
                $"Training type '{normalizedName}' already exists.");
        }

        return TrainingTypeOperationResult<TrainingTypeResponse>.Success(
            new TrainingTypeResponse
            {
                Id = entity.Id,
                Name = entity.Name
            });
    }

    public async Task<TrainingTypeOperationResult<int>> CreateBulkAsync(
        IReadOnlyList<CreateTrainingTypeRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests.Count == 0)
        {
            return TrainingTypeOperationResult<int>.ValidationError("At least one training type is required.");
        }

        var normalizedNames = requests
            .Select(x => StorageTextNormalizer.NormalizeKey(x.Name))
            .ToList();

        if (normalizedNames.Any(string.IsNullOrWhiteSpace))
        {
            return TrainingTypeOperationResult<int>.ValidationError("Training type names cannot be blank.");
        }

        var duplicateNames = normalizedNames
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .OrderBy(x => x)
            .ToList();

        if (duplicateNames.Count > 0)
        {
            return TrainingTypeOperationResult<int>.Conflict(
                $"Duplicate training types in request: {string.Join(", ", duplicateNames)}.");
        }

        var existingNames = await dbContext.TrainingTypes
            .AsNoTracking()
            .Where(x => normalizedNames.Contains(x.Name))
            .Select(x => x.Name)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        if (existingNames.Count > 0)
        {
            return TrainingTypeOperationResult<int>.Conflict(
                $"Training types already exist: {string.Join(", ", existingNames)}.");
        }

        var entities = normalizedNames
            .Select(x => new TrainingType
            {
                Name = x
            })
            .ToList();

        dbContext.TrainingTypes.AddRange(entities);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return TrainingTypeOperationResult<int>.Conflict(
                "One or more training types already exist. Refresh and retry.");
        }

        return TrainingTypeOperationResult<int>.Success(entities.Count);
    }

    public async Task<TrainingTypeOperationResult<TrainingTypeResponse>> UpdateAsync(
        int id,
        UpdateTrainingTypeRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TrainingTypeOperationResult<TrainingTypeResponse>.ValidationError("Name is required.");
        }

        var entity = await dbContext.TrainingTypes
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return TrainingTypeOperationResult<TrainingTypeResponse>.NotFound(
                $"Training type with id '{id}' was not found.");
        }

        var normalizedName = StorageTextNormalizer.NormalizeKey(request.Name);
        var conflictExists = await dbContext.TrainingTypes
            .AsNoTracking()
            .AnyAsync(x => x.Name == normalizedName && x.Id != id, cancellationToken);
        if (conflictExists)
        {
            return TrainingTypeOperationResult<TrainingTypeResponse>.Conflict(
                $"Training type '{normalizedName}' already exists.");
        }

        entity.Name = normalizedName;
        await dbContext.SaveChangesAsync(cancellationToken);

        return TrainingTypeOperationResult<TrainingTypeResponse>.Success(
            new TrainingTypeResponse
            {
                Id = entity.Id,
                Name = entity.Name
            });
    }

    public async Task<TrainingTypeOperationResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TrainingTypes
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return TrainingTypeOperationResult.NotFound($"Training type with id '{id}' was not found.");
        }

        dbContext.TrainingTypes.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TrainingTypeOperationResult.Success();
    }
}
