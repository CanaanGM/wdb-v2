using Api.Application.Text;
using Api.Features.Muscles.Contracts;
using Domain.Muscles;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Api.Features.Muscles.Services;

public sealed class MusclesService(WorkoutLogDbContext dbContext) : IMusclesService
{
    public async Task<List<MuscleResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Muscles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(MapToResponse())
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MuscleResponse>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
    {
        var normalizedSearchTerm = searchTerm.Trim();

        return await dbContext.Muscles
            .AsNoTracking()
            .Where(x => EF.Functions.ILike(x.Name, $"%{normalizedSearchTerm}%"))
            .OrderBy(x => x.Name)
            .Select(MapToResponse())
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MuscleResponse>> GetByGroupAsync(string groupName, CancellationToken cancellationToken)
    {
        var normalizedGroup = StorageTextNormalizer.NormalizeKey(groupName);

        return await dbContext.Muscles
            .AsNoTracking()
            .Where(x => x.MuscleGroup == normalizedGroup)
            .OrderBy(x => x.Name)
            .Select(MapToResponse())
            .ToListAsync(cancellationToken);
    }

    public async Task<CreateMusclesBulkResult> CreateBulkAsync(
        List<CreateMuscleRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests is null || requests.Count == 0)
        {
            return CreateMusclesBulkResult.ValidationError("At least one muscle is required.");
        }

        var normalizedNames = new List<string>(requests.Count);
        normalizedNames.AddRange(requests.Select(x => StorageTextNormalizer.NormalizeKey(x.Name)));

        var duplicateNamesInRequest = normalizedNames
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .OrderBy(x => x)
            .ToList();

        if (duplicateNamesInRequest.Count > 0)
        {
            return CreateMusclesBulkResult.Conflict(
                $"Duplicate muscle names in request: {string.Join(", ", duplicateNamesInRequest)}.");
        }

        var normalizedLookup = normalizedNames
            .Distinct()
            .ToList();

        var existingNames = await dbContext.Muscles
            .AsNoTracking()
            .Where(x => normalizedLookup.Contains(x.Name))
            .Select(x => x.Name)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        if (existingNames.Count > 0)
        {
            return CreateMusclesBulkResult.Conflict(
                $"Muscles already exist: {string.Join(", ", existingNames)}.");
        }

        var muscles = requests
            .Select(request => MapToEntity(request))
            .ToList();

        dbContext.Muscles.AddRange(muscles);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return CreateMusclesBulkResult.Conflict(
                "One or more muscles already exist. Refresh and retry the bulk request.");
        }

        return CreateMusclesBulkResult.Success(muscles.Count);
    }

    private static Expression<Func<Muscle, MuscleResponse>> MapToResponse()
    {
        return x => new MuscleResponse
        {
            Id = x.Id,
            Name = x.Name,
            MuscleGroup = x.MuscleGroup,
            Function = x.Function,
            WikiPageUrl = x.WikiPageUrl
        };
    }

    private static Muscle MapToEntity(CreateMuscleRequest request)
    {
        return new Muscle
        {
            Name = StorageTextNormalizer.NormalizeKey(request.Name),
            MuscleGroup = StorageTextNormalizer.NormalizeKey(request.MuscleGroup),
            Function = StorageTextNormalizer.NormalizeOptionalText(request.Function),
            WikiPageUrl = StorageTextNormalizer.NormalizeOptionalText(request.WikiPageUrl)
        };
    }
}
