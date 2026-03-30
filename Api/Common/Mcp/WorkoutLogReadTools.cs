using System.ComponentModel;
using Api.Application.Contracts.Querying;
using Api.Features.Equipments.Contracts;
using Api.Features.Equipments.Services;
using Api.Features.Exercises.Contracts;
using Api.Features.Exercises.Services;
using Api.Features.Muscles.Contracts;
using Api.Features.Muscles.Services;
using Api.Features.UserExerciseStats.Contracts;
using Api.Features.UserExerciseStats.Services;
using Api.Features.WorkoutBlocks.Contracts;
using Api.Features.WorkoutBlocks.Services;
using Api.Features.Workouts.Contracts;
using Api.Features.Workouts.Services;
using ModelContextProtocol.Server;

namespace Api.Common.Mcp;

[McpServerToolType]
public sealed class WorkoutLogReadTools(
    IExercisesService exercisesService,
    IMusclesService musclesService,
    IEquipmentsService equipmentsService,
    IWorkoutsService workoutsService,
    IWorkoutBlocksService workoutBlocksService,
    IUserExerciseStatsService userExerciseStatsService,
    IHostEnvironment hostEnvironment)
{
    [McpServerTool(Name = "search_exercises", ReadOnly = true, OpenWorld = false)]
    [Description("Search exercises with pagination and optional filters.")]
    public async Task<PagedResponse<ExerciseResponse>> SearchExercisesAsync(
        [Description("1-based page number.")] int pageNumber = 1,
        [Description("Page size between 1 and 100.")] int pageSize = 20,
        [Description("Optional free-text search across exercise name/description.")] string? search = null,
        [Description("Optional exercise difficulty filter (0..5).")]
        int? difficulty = null,
        [Description("Optional muscle name filter.")] string? muscleName = null,
        [Description("Optional muscle group filter.")] string? muscleGroup = null,
        [Description("Optional primary-muscle filter.")] bool? isPrimary = null,
        CancellationToken cancellationToken = default)
    {
        var request = new GetExercisesRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search,
            Difficulty = difficulty,
            MuscleName = muscleName,
            MuscleGroup = muscleGroup,
            IsPrimary = isPrimary
        };

        return await exercisesService.GetAllAsync(request, cancellationToken);
    }

    [McpServerTool(Name = "get_exercise_by_id", ReadOnly = true, OpenWorld = false)]
    [Description("Get one exercise by its id.")]
    public async Task<ExerciseResponse?> GetExerciseByIdAsync(
        [Description("Exercise id.")] int id,
        CancellationToken cancellationToken = default)
    {
        return await exercisesService.GetByIdAsync(id, cancellationToken);
    }

    [McpServerTool(Name = "get_muscles", ReadOnly = true, OpenWorld = false)]
    [Description("Get all muscles.")]
    public async Task<List<MuscleResponse>> GetMusclesAsync(CancellationToken cancellationToken = default)
    {
        return await musclesService.GetAllAsync(cancellationToken);
    }

    [McpServerTool(Name = "search_muscles", ReadOnly = true, OpenWorld = false)]
    [Description("Search muscles by text fragment.")]
    public async Task<List<MuscleResponse>> SearchMusclesAsync(
        [Description("Search term.")] string searchTerm,
        CancellationToken cancellationToken = default)
    {
        return await musclesService.SearchAsync(searchTerm, cancellationToken);
    }

    [McpServerTool(Name = "get_muscles_by_group", ReadOnly = true, OpenWorld = false)]
    [Description("Get muscles by group name.")]
    public async Task<List<MuscleResponse>> GetMusclesByGroupAsync(
        [Description("Muscle group name.")] string groupName,
        CancellationToken cancellationToken = default)
    {
        return await musclesService.GetByGroupAsync(groupName, cancellationToken);
    }

    [McpServerTool(Name = "get_equipments", ReadOnly = true, OpenWorld = false)]
    [Description("Get all equipment items.")]
    public async Task<List<EquipmentResponse>> GetEquipmentsAsync(CancellationToken cancellationToken = default)
    {
        return await equipmentsService.GetAllAsync(cancellationToken);
    }

    [McpServerTool(Name = "search_equipments", ReadOnly = true, OpenWorld = false)]
    [Description("Search equipment by text fragment.")]
    public async Task<List<EquipmentResponse>> SearchEquipmentsAsync(
        [Description("Search term.")] string searchTerm,
        CancellationToken cancellationToken = default)
    {
        return await equipmentsService.SearchAsync(searchTerm, cancellationToken);
    }

    [McpServerTool(Name = "get_equipment_by_name", ReadOnly = true, OpenWorld = false)]
    [Description("Get one equipment item by its unique name.")]
    public async Task<EquipmentResponse?> GetEquipmentByNameAsync(
        [Description("Equipment name.")] string name,
        CancellationToken cancellationToken = default)
    {
        return await equipmentsService.GetByNameAsync(name, cancellationToken);
    }

    [McpServerTool(Name = "search_workouts", ReadOnly = true, OpenWorld = false)]
    [Description("Search workouts for a given user id. Development-only tool.")]
    public async Task<PagedResponse<WorkoutResponse>> SearchWorkoutsAsync(
        [Description("User id whose workouts will be queried.")] int userId,
        [Description("1-based page number.")] int pageNumber = 1,
        [Description("Page size between 1 and 100.")] int pageSize = 20,
        [Description("Optional performed-at lower bound (UTC).")]
        DateTime? fromUtc = null,
        [Description("Optional performed-at upper bound (UTC).")]
        DateTime? toUtc = null,
        [Description("Optional free-text search across feeling/notes.")] string? search = null,
        [Description("Optional exercise id filter.")] int? exerciseId = null,
        [Description("Optional minimum mood (0..10).")]
        int? minMood = null,
        [Description("Optional maximum mood (0..10).")]
        int? maxMood = null,
        CancellationToken cancellationToken = default)
    {
        EnsureDevelopmentOnly();

        var request = new SearchWorkoutsRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Search = search,
            ExerciseId = exerciseId,
            MinMood = minMood,
            MaxMood = maxMood
        };

        return await workoutsService.SearchAsync(userId, request, cancellationToken);
    }

    [McpServerTool(Name = "get_workout_by_id", ReadOnly = true, OpenWorld = false)]
    [Description("Get one workout by id for a given user id. Development-only tool.")]
    public async Task<WorkoutResponse?> GetWorkoutByIdAsync(
        [Description("User id whose workout will be queried.")] int userId,
        [Description("Workout id.")] int workoutId,
        CancellationToken cancellationToken = default)
    {
        EnsureDevelopmentOnly();
        return await workoutsService.GetByIdAsync(userId, workoutId, cancellationToken);
    }

    [McpServerTool(Name = "search_workout_blocks", ReadOnly = true, OpenWorld = false)]
    [Description("Search workout blocks for a given user id. Development-only tool.")]
    public async Task<PagedResponse<WorkoutBlockResponse>> SearchWorkoutBlocksAsync(
        [Description("User id whose blocks will be queried.")] int userId,
        [Description("1-based page number.")] int pageNumber = 1,
        [Description("Page size between 1 and 100.")] int pageSize = 20,
        [Description("Optional free-text search across name/instructions.")] string? search = null,
        CancellationToken cancellationToken = default)
    {
        EnsureDevelopmentOnly();

        var request = new SearchWorkoutBlocksRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search
        };

        return await workoutBlocksService.SearchAsync(userId, request, cancellationToken);
    }

    [McpServerTool(Name = "get_workout_block_by_id", ReadOnly = true, OpenWorld = false)]
    [Description("Get one workout block by id for a given user id. Development-only tool.")]
    public async Task<WorkoutBlockResponse?> GetWorkoutBlockByIdAsync(
        [Description("User id whose block will be queried.")] int userId,
        [Description("Workout block id.")] int blockId,
        CancellationToken cancellationToken = default)
    {
        EnsureDevelopmentOnly();
        return await workoutBlocksService.GetByIdAsync(userId, blockId, cancellationToken);
    }

    [McpServerTool(Name = "search_user_exercise_stats", ReadOnly = true, OpenWorld = false)]
    [Description("Search user exercise stats for a given user id. Development-only tool.")]
    public async Task<PagedResponse<UserExerciseStatResponse>> SearchUserExerciseStatsAsync(
        [Description("User id whose exercise stats will be queried.")] int userId,
        [Description("1-based page number.")] int pageNumber = 1,
        [Description("Page size between 1 and 100.")] int pageSize = 20,
        [Description("Optional search on exercise name.")] string? search = null,
        [Description("Optional exact exercise id filter.")] int? exerciseId = null,
        CancellationToken cancellationToken = default)
    {
        EnsureDevelopmentOnly();

        var request = new SearchUserExerciseStatsRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search,
            ExerciseId = exerciseId
        };

        return await userExerciseStatsService.SearchAsync(userId, request, cancellationToken);
    }

    private void EnsureDevelopmentOnly()
    {
        if (!hostEnvironment.IsDevelopment())
        {
            throw new InvalidOperationException("Workout MCP tools are available in Development only.");
        }
    }
}
