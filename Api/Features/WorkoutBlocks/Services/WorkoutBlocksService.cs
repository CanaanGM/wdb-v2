using System.Linq.Expressions;
using Api.Application.Contracts.Querying;
using Api.Application.Querying;
using Api.Application.Text;
using Api.Features.WorkoutBlocks.Contracts;
using Domain.WorkoutBlocks;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.WorkoutBlocks.Services;

public sealed class WorkoutBlocksService(WorkoutLogDbContext dbContext) : IWorkoutBlocksService
{
    public async Task<PagedResponse<WorkoutBlockResponse>> SearchAsync(
        int userId,
        SearchWorkoutBlocksRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : request.Search.Trim();

        var query = dbContext.WorkoutBlocks
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .WhereIf(
                !string.IsNullOrWhiteSpace(normalizedSearch),
                x => EF.Functions.ILike(x.Name, $"%{normalizedSearch}%")
                     || (x.Instructions != null && EF.Functions.ILike(x.Instructions, $"%{normalizedSearch}%")))
            .OrderBy(x => x.OrderNumber)
            .ThenBy(x => x.Name)
            .Select(MapToResponseExpression());

        return await query.ToPagedResponseAsync(request.PageNumber, request.PageSize, cancellationToken);
    }

    public async Task<WorkoutBlockResponse?> GetByIdAsync(
        int userId,
        int workoutBlockId,
        CancellationToken cancellationToken)
    {
        return await dbContext.WorkoutBlocks
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Id == workoutBlockId)
            .Select(MapToResponseExpression())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<WorkoutBlockOperationResult<WorkoutBlockResponse>> CreateAsync(
        int userId,
        CreateWorkoutBlockRequest request,
        CancellationToken cancellationToken)
    {
        if (request.BlockExercises.Count == 0)
        {
            return WorkoutBlockOperationResult<WorkoutBlockResponse>.ValidationError("At least one block exercise is required.");
        }

        var exerciseValidation = await ValidateExerciseIdsAsync(
            request.BlockExercises.Select(x => x.ExerciseId),
            cancellationToken);

        if (exerciseValidation.Error is not null)
        {
            return WorkoutBlockOperationResult<WorkoutBlockResponse>.ValidationError(exerciseValidation.Error);
        }

        var workoutBlock = new WorkoutBlock
        {
            UserId = userId,
            Name = StorageTextNormalizer.NormalizeKey(request.Name),
            Sets = request.Sets,
            RestInSeconds = request.RestInSeconds,
            OrderNumber = request.OrderNumber,
            Instructions = StorageTextNormalizer.NormalizeOptionalText(request.Instructions),
            CreatedAtUtc = DateTime.UtcNow,
            BlockExercises = MapBlockExercises(request.BlockExercises)
        };

        dbContext.WorkoutBlocks.Add(workoutBlock);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await GetByIdAsync(userId, workoutBlock.Id, cancellationToken);
        if (created is null)
        {
            throw new InvalidOperationException("Workout block creation failed.");
        }

        return WorkoutBlockOperationResult<WorkoutBlockResponse>.Success(created);
    }

    public async Task<WorkoutBlockOperationResult<int>> CreateBulkAsync(
        int userId,
        List<CreateWorkoutBlockRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests.Count == 0)
        {
            return WorkoutBlockOperationResult<int>.ValidationError("At least one workout block is required.");
        }

        if (requests.Any(x => x.BlockExercises.Count == 0))
        {
            return WorkoutBlockOperationResult<int>.ValidationError("Every workout block must have at least one exercise.");
        }

        var exerciseValidation = await ValidateExerciseIdsAsync(
            requests.SelectMany(x => x.BlockExercises).Select(x => x.ExerciseId),
            cancellationToken);

        if (exerciseValidation.Error is not null)
        {
            return WorkoutBlockOperationResult<int>.ValidationError(exerciseValidation.Error);
        }

        var now = DateTime.UtcNow;

        var blocks = requests
            .Select(request => new WorkoutBlock
            {
                UserId = userId,
                Name = StorageTextNormalizer.NormalizeKey(request.Name),
                Sets = request.Sets,
                RestInSeconds = request.RestInSeconds,
                OrderNumber = request.OrderNumber,
                Instructions = StorageTextNormalizer.NormalizeOptionalText(request.Instructions),
                CreatedAtUtc = now,
                BlockExercises = MapBlockExercises(request.BlockExercises)
            })
            .ToList();

        dbContext.WorkoutBlocks.AddRange(blocks);
        await dbContext.SaveChangesAsync(cancellationToken);

        return WorkoutBlockOperationResult<int>.Success(blocks.Count);
    }

    public async Task<WorkoutBlockOperationResult<WorkoutBlockResponse>> UpdateAsync(
        int userId,
        int workoutBlockId,
        UpdateWorkoutBlockRequest request,
        CancellationToken cancellationToken)
    {
        if (request.BlockExercises.Count == 0)
        {
            return WorkoutBlockOperationResult<WorkoutBlockResponse>.ValidationError("At least one block exercise is required.");
        }

        var workoutBlock = await dbContext.WorkoutBlocks
            .Include(x => x.BlockExercises)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Id == workoutBlockId, cancellationToken);

        if (workoutBlock is null)
        {
            return WorkoutBlockOperationResult<WorkoutBlockResponse>.NotFound($"Workout block with id '{workoutBlockId}' was not found.");
        }

        var exerciseValidation = await ValidateExerciseIdsAsync(
            request.BlockExercises.Select(x => x.ExerciseId),
            cancellationToken);

        if (exerciseValidation.Error is not null)
        {
            return WorkoutBlockOperationResult<WorkoutBlockResponse>.ValidationError(exerciseValidation.Error);
        }

        workoutBlock.Name = StorageTextNormalizer.NormalizeKey(request.Name);
        workoutBlock.Sets = request.Sets;
        workoutBlock.RestInSeconds = request.RestInSeconds;
        workoutBlock.OrderNumber = request.OrderNumber;
        workoutBlock.Instructions = StorageTextNormalizer.NormalizeOptionalText(request.Instructions);

        dbContext.WorkoutBlockExercises.RemoveRange(workoutBlock.BlockExercises);
        workoutBlock.BlockExercises = MapBlockExercises(request.BlockExercises);

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await GetByIdAsync(userId, workoutBlock.Id, cancellationToken);
        if (updated is null)
        {
            throw new InvalidOperationException("Workout block update failed.");
        }

        return WorkoutBlockOperationResult<WorkoutBlockResponse>.Success(updated);
    }

    public async Task<WorkoutBlockOperationResult> DeleteAsync(int userId, int workoutBlockId, CancellationToken cancellationToken)
    {
        var workoutBlock = await dbContext.WorkoutBlocks
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Id == workoutBlockId, cancellationToken);

        if (workoutBlock is null)
        {
            return WorkoutBlockOperationResult.NotFound($"Workout block with id '{workoutBlockId}' was not found.");
        }

        dbContext.WorkoutBlocks.Remove(workoutBlock);
        await dbContext.SaveChangesAsync(cancellationToken);

        return WorkoutBlockOperationResult.Success();
    }

    private async Task<(List<int> ExerciseIds, string? Error)> ValidateExerciseIdsAsync(
        IEnumerable<int> exerciseIds,
        CancellationToken cancellationToken)
    {
        var ids = exerciseIds
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return ([], "At least one exercise is required.");
        }

        var existingExerciseIds = await dbContext.Exercises
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var missingExerciseIds = ids
            .Except(existingExerciseIds)
            .OrderBy(x => x)
            .ToList();

        if (missingExerciseIds.Count > 0)
        {
            return ([], $"Exercises not found: {string.Join(", ", missingExerciseIds)}.");
        }

        return (ids, null);
    }

    private static List<WorkoutBlockExercise> MapBlockExercises(IEnumerable<WorkoutBlockExerciseRequest> requests)
    {
        var now = DateTime.UtcNow;

        return requests
            .Select(x => new WorkoutBlockExercise
            {
                ExerciseId = x.ExerciseId,
                OrderNumber = x.OrderNumber,
                Instructions = StorageTextNormalizer.NormalizeOptionalText(x.Instructions),
                Repetitions = x.Repetitions,
                TimerInSeconds = x.TimerInSeconds,
                DistanceInMeters = x.DistanceInMeters,
                CreatedAtUtc = now
            })
            .ToList();
    }

    private static Expression<Func<WorkoutBlock, WorkoutBlockResponse>> MapToResponseExpression()
    {
        return x => new WorkoutBlockResponse
        {
            Id = x.Id,
            UserId = x.UserId,
            Name = x.Name,
            Sets = x.Sets,
            RestInSeconds = x.RestInSeconds,
            OrderNumber = x.OrderNumber,
            Instructions = x.Instructions,
            CreatedAtUtc = x.CreatedAtUtc,
            BlockExercises = x.BlockExercises
                .OrderBy(e => e.OrderNumber)
                .Select(e => new WorkoutBlockExerciseResponse
                {
                    Id = e.Id,
                    ExerciseId = e.ExerciseId,
                    ExerciseName = e.Exercise.Name,
                    OrderNumber = e.OrderNumber,
                    Instructions = e.Instructions,
                    Repetitions = e.Repetitions,
                    TimerInSeconds = e.TimerInSeconds,
                    DistanceInMeters = e.DistanceInMeters
                })
                .ToList()
        };
    }
}
