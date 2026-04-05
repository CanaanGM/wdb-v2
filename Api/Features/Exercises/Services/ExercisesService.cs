using Api.Application.Contracts.Querying;
using Api.Application.Querying;
using Api.Application.Text;
using Api.Features.Exercises.Contracts;
using Domain.Exercises;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Api.Features.Exercises.Services;

public sealed class ExercisesService(WorkoutLogDbContext dbContext) : IExercisesService
{
    public async Task<PagedResponse<ExerciseResponse>> GetAllAsync(
        GetExercisesRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : request.Search.Trim();

        var normalizedMuscleName = string.IsNullOrWhiteSpace(request.MuscleName)
            ? null
            : StorageTextNormalizer.NormalizeKey(request.MuscleName);

        var normalizedMuscleGroup = string.IsNullOrWhiteSpace(request.MuscleGroup)
            ? null
            : StorageTextNormalizer.NormalizeKey(request.MuscleGroup);

        var normalizedTrainingTypeName = string.IsNullOrWhiteSpace(request.TrainingTypeName)
            ? null
            : StorageTextNormalizer.NormalizeKey(request.TrainingTypeName);

        var difficulty = request.Difficulty;
        var isPrimary = request.IsPrimary;
        var difficultyValue = difficulty ?? default;
        var isPrimaryValue = isPrimary ?? default;

        var query = dbContext.Exercises
            .AsNoTracking()
            .WhereIf(
                !string.IsNullOrWhiteSpace(normalizedSearch),
                x => EF.Functions.ILike(x.Name, $"%{normalizedSearch}%")
                     || (x.Description != null && EF.Functions.ILike(x.Description, $"%{normalizedSearch}%")))
            .WhereIf(
                difficulty.HasValue,
                x => x.Difficulty == difficultyValue)
            .WhereIf(
                normalizedMuscleName is not null,
                x => x.ExerciseMuscles.Any(em => em.Muscle.Name == normalizedMuscleName))
            .WhereIf(
                normalizedMuscleGroup is not null,
                x => x.ExerciseMuscles.Any(em => em.Muscle.MuscleGroup == normalizedMuscleGroup))
            .WhereIf(
                normalizedTrainingTypeName is not null,
                x => x.ExerciseTrainingTypes.Any(et => et.TrainingType.Name == normalizedTrainingTypeName))
            .WhereIf(
                isPrimary.HasValue,
                x => x.ExerciseMuscles.Any(em => em.IsPrimary == isPrimaryValue))
            .OrderBy(x => x.Name)
            .Select(MapToResponseExpression());

        return await query.ToPagedResponseAsync(request.PageNumber, request.PageSize, cancellationToken);
    }

    public async Task<ExerciseResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Exercises
            .AsNoTracking()
            .Include(x => x.HowTos)
            .Include(x => x.ExerciseMuscles)
            .ThenInclude(x => x.Muscle)
            .Include(x => x.ExerciseTrainingTypes)
            .ThenInclude(x => x.TrainingType)
            .Where(x => x.Id == id)
            .Select(MapToResponseExpression())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CreateExerciseResult> CreateAsync(CreateExerciseRequest request, CancellationToken cancellationToken)
    {
        var normalizedName = StorageTextNormalizer.NormalizeKey(request.Name);

        var existingName = await dbContext.Exercises
            .AsNoTracking()
            .Where(x => x.Name == normalizedName)
            .Select(x => x.Name)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingName is not null)
        {
            return CreateExerciseResult.Conflict($"Exercise with name '{existingName}' already exists.");
        }

        var muscleResolution = await ResolveMuscleIdsAsync(
            request.ExerciseMuscles.Select(x => x.MuscleName),
            cancellationToken);

        if (muscleResolution.MissingMuscles.Count > 0)
        {
            return CreateExerciseResult.ValidationError(
                $"Muscles not found: {string.Join(", ", muscleResolution.MissingMuscles)}.");
        }

        var trainingTypeResolution = await ResolveTrainingTypeIdsAsync(request.TrainingTypes, cancellationToken);
        if (trainingTypeResolution.MissingTrainingTypes.Count > 0)
        {
            return CreateExerciseResult.ValidationError(
                $"Training types not found: {string.Join(", ", trainingTypeResolution.MissingTrainingTypes)}.");
        }

        var exercise = MapToEntity(
            request,
            normalizedName,
            muscleResolution.MuscleIdByName,
            trainingTypeResolution.TrainingTypeIdByName);

        dbContext.Exercises.Add(exercise);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return CreateExerciseResult.Conflict($"Exercise with name '{normalizedName}' already exists.");
        }

        var response = await dbContext.Exercises
            .AsNoTracking()
            .Include(x => x.HowTos)
            .Include(x => x.ExerciseMuscles)
            .ThenInclude(x => x.Muscle)
            .Include(x => x.ExerciseTrainingTypes)
            .ThenInclude(x => x.TrainingType)
            .Where(x => x.Id == exercise.Id)
            .Select(MapToResponseExpression())
            .SingleAsync(cancellationToken);

        return CreateExerciseResult.Success(response);
    }

    public async Task<CreateExercisesBulkResult> CreateBulkAsync(
        List<CreateExerciseRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests is null || requests.Count == 0)
        {
            return CreateExercisesBulkResult.ValidationError("At least one exercise is required.");
        }

        var normalizedNames = new List<string>(requests.Count);
        var allMuscleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allTrainingTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < requests.Count; i++)
        {
            normalizedNames.Add(StorageTextNormalizer.NormalizeKey(requests[i].Name));

            foreach (var exerciseMuscle in requests[i].ExerciseMuscles)
            {
                allMuscleNames.Add(StorageTextNormalizer.NormalizeKey(exerciseMuscle.MuscleName));
            }

            foreach (var trainingType in requests[i].TrainingTypes)
            {
                allTrainingTypeNames.Add(StorageTextNormalizer.NormalizeKey(trainingType));
            }
        }

        var duplicateNamesInRequest = normalizedNames
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .OrderBy(x => x)
            .ToList();

        if (duplicateNamesInRequest.Count > 0)
        {
            return CreateExercisesBulkResult.Conflict(
                $"Duplicate exercise names in request: {string.Join(", ", duplicateNamesInRequest)}.");
        }

        var normalizedLookup = normalizedNames
            .Distinct()
            .ToList();

        var existingNames = await dbContext.Exercises
            .AsNoTracking()
            .Where(x => normalizedLookup.Contains(x.Name))
            .Select(x => x.Name)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        if (existingNames.Count > 0)
        {
            return CreateExercisesBulkResult.Conflict(
                $"Exercises already exist: {string.Join(", ", existingNames)}.");
        }

        var muscleResolution = await ResolveMuscleIdsAsync(allMuscleNames, cancellationToken);
        if (muscleResolution.MissingMuscles.Count > 0)
        {
            return CreateExercisesBulkResult.ValidationError(
                $"Muscles not found: {string.Join(", ", muscleResolution.MissingMuscles)}.");
        }

        var trainingTypeResolution = await ResolveTrainingTypeIdsAsync(allTrainingTypeNames, cancellationToken);
        if (trainingTypeResolution.MissingTrainingTypes.Count > 0)
        {
            return CreateExercisesBulkResult.ValidationError(
                $"Training types not found: {string.Join(", ", trainingTypeResolution.MissingTrainingTypes)}.");
        }

        var exercises = requests
            .Select((request, index) => MapToEntity(
                request,
                normalizedNames[index],
                muscleResolution.MuscleIdByName,
                trainingTypeResolution.TrainingTypeIdByName))
            .ToList();

        dbContext.Exercises.AddRange(exercises);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return CreateExercisesBulkResult.Conflict(
                "One or more exercises already exist. Refresh and retry the bulk request.");
        }

        return CreateExercisesBulkResult.Success(exercises.Count);
    }

    private static Expression<Func<Exercise, ExerciseResponse>> MapToResponseExpression()
    {
        return x => new ExerciseResponse
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            HowTo = x.HowTo,
            Difficulty = x.Difficulty,
            TrainingTypes = x.ExerciseTrainingTypes
                .Select(et => et.TrainingType.Name)
                .OrderBy(name => name)
                .ToList(),
            HowTos = x.HowTos
                .OrderBy(h => h.Id)
                .Select(h => new ExerciseHowToResponse
                {
                    Id = h.Id,
                    Name = h.Name,
                    Url = h.Url
                })
                .ToList(),
            ExerciseMuscles = x.ExerciseMuscles
                .OrderBy(em => em.Muscle.Name)
                .Select(em => new ExerciseMuscleResponse
                {
                    MuscleId = em.MuscleId,
                    Name = em.Muscle.Name,
                    MuscleGroup = em.Muscle.MuscleGroup,
                    Function = em.Muscle.Function,
                    WikiPageUrl = em.Muscle.WikiPageUrl,
                    IsPrimary = em.IsPrimary
                })
                .ToList()
        };
    }

    private static Exercise MapToEntity(
        CreateExerciseRequest request,
        string normalizedName,
        IReadOnlyDictionary<string, int> muscleIdByName,
        IReadOnlyDictionary<string, int> trainingTypeIdByName)
    {
        var howTos = request.HowTos ?? [];
        var exerciseMuscles = request.ExerciseMuscles ?? [];
        var trainingTypes = request.TrainingTypes ?? [];

        return new Exercise
        {
            Name = normalizedName,
            Description = StorageTextNormalizer.NormalizeOptionalText(request.Description),
            HowTo = StorageTextNormalizer.NormalizeOptionalText(request.HowTo),
            Difficulty = request.Difficulty,
            HowTos = howTos
                .Select(x => new ExerciseHowTo
                {
                    Name = StorageTextNormalizer.NormalizeKey(x.Name),
                    Url = x.Url.Trim()
                })
                .ToList(),
            ExerciseMuscles = exerciseMuscles
                .Select(x => new ExerciseMuscle
                {
                    MuscleId = muscleIdByName[StorageTextNormalizer.NormalizeKey(x.MuscleName)],
                    IsPrimary = x.IsPrimary
                })
                .ToList(),
            ExerciseTrainingTypes = trainingTypes
                .Select(x => new ExerciseTrainingType
                {
                    TrainingTypeId = trainingTypeIdByName[StorageTextNormalizer.NormalizeKey(x)]
                })
                .ToList()
        };
    }

    private async Task<(Dictionary<string, int> MuscleIdByName, List<string> MissingMuscles)> ResolveMuscleIdsAsync(
        IEnumerable<string> muscleNames,
        CancellationToken cancellationToken)
    {
        var normalizedMuscleNames = muscleNames
            .Select(StorageTextNormalizer.NormalizeKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var muscles = await dbContext.Muscles
            .AsNoTracking()
            .Where(x => normalizedMuscleNames.Contains(x.Name))
            .Select(x => new
            {
                x.Id,
                x.Name
            })
            .ToListAsync(cancellationToken);

        var muscleIdByName = muscles.ToDictionary(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase);
        var missingMuscles = normalizedMuscleNames
            .Where(x => !muscleIdByName.ContainsKey(x))
            .OrderBy(x => x)
            .ToList();

        return (muscleIdByName, missingMuscles);
    }

    private async Task<(Dictionary<string, int> TrainingTypeIdByName, List<string> MissingTrainingTypes)>
        ResolveTrainingTypeIdsAsync(
            IEnumerable<string> trainingTypeNames,
            CancellationToken cancellationToken)
    {
        var normalizedTrainingTypeNames = trainingTypeNames
            .Select(StorageTextNormalizer.NormalizeKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var trainingTypes = await dbContext.TrainingTypes
            .AsNoTracking()
            .Where(x => normalizedTrainingTypeNames.Contains(x.Name))
            .Select(x => new
            {
                x.Id,
                x.Name
            })
            .ToListAsync(cancellationToken);

        var trainingTypeIdByName = trainingTypes.ToDictionary(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase);
        var missingTrainingTypes = normalizedTrainingTypeNames
            .Where(x => !trainingTypeIdByName.ContainsKey(x))
            .OrderBy(x => x)
            .ToList();

        return (trainingTypeIdByName, missingTrainingTypes);
    }
}
