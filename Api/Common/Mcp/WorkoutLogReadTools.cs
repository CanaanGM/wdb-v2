using System.ComponentModel;
using Api.Application.Contracts.Querying;
using Api.Features.Equipments.Contracts;
using Api.Features.Equipments.Services;
using Api.Features.Exercises.Contracts;
using Api.Features.Exercises.Services;
using Api.Features.Muscles.Contracts;
using Api.Features.Muscles.Services;
using ModelContextProtocol.Server;

namespace Api.Common.Mcp;

[McpServerToolType]
public sealed class WorkoutLogReadTools(
    IExercisesService exercisesService,
    IMusclesService musclesService,
    IEquipmentsService equipmentsService)
{
    [McpServerTool(Name = "search_exercises", ReadOnly = true, OpenWorld = false)]
    [Description("Search exercises with pagination and optional filters.")]
    public async Task<PagedResponse<ExerciseResponse>> SearchExercisesAsync(
        [Description("1-based page number.")] int pageNumber = 1,
        [Description("Page size between 1 and 100.")] int pageSize = 20,
        [Description("Optional free-text search across exercise name/description.")] string? search = null,
        [Description("Optional exercise difficulty filter (0..5).")] int? difficulty = null,
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
}
