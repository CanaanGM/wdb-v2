using System.Linq.Expressions;
using Api.Application.Text;
using Api.Features.Equipments.Contracts;
using Domain.Equipments;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Equipments.Services;

public sealed class EquipmentsService(WorkoutLogDbContext dbContext) : IEquipmentsService
{
    public async Task<List<EquipmentResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Equipments
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(MapToResponseExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EquipmentResponse>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
    {
        var normalizedSearchTerm = searchTerm.Trim();

        return await dbContext.Equipments
            .AsNoTracking()
            .Where(x =>
                EF.Functions.ILike(x.Name, $"%{normalizedSearchTerm}%")
                || (x.Description != null && EF.Functions.ILike(x.Description, $"%{normalizedSearchTerm}%"))
                || (x.HowTo != null && EF.Functions.ILike(x.HowTo, $"%{normalizedSearchTerm}%")))
            .OrderBy(x => x.Name)
            .Select(MapToResponseExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<EquipmentResponse?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = StorageTextNormalizer.NormalizeKey(name);

        return await dbContext.Equipments
            .AsNoTracking()
            .Where(x => x.Name == normalizedName)
            .Select(MapToResponseExpression())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CreateEquipmentResult> CreateAsync(CreateEquipmentRequest request, CancellationToken cancellationToken)
    {
        var normalizedName = StorageTextNormalizer.NormalizeKey(request.Name);

        var existingName = await dbContext.Equipments
            .AsNoTracking()
            .Where(x => x.Name == normalizedName)
            .Select(x => x.Name)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingName is not null)
        {
            return CreateEquipmentResult.Conflict($"Equipment with name '{existingName}' already exists.");
        }

        var equipment = MapToEntity(request, normalizedName);
        dbContext.Equipments.Add(equipment);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return CreateEquipmentResult.Conflict($"Equipment with name '{normalizedName}' already exists.");
        }

        var response = await dbContext.Equipments
            .AsNoTracking()
            .Where(x => x.Id == equipment.Id)
            .Select(MapToResponseExpression())
            .SingleAsync(cancellationToken);

        return CreateEquipmentResult.Success(response);
    }

    public async Task<CreateEquipmentsBulkResult> CreateBulkAsync(
        List<CreateEquipmentRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests is null || requests.Count == 0)
        {
            return CreateEquipmentsBulkResult.ValidationError("At least one equipment item is required.");
        }

        var normalizedNames = requests
            .Select(x => StorageTextNormalizer.NormalizeKey(x.Name))
            .ToList();

        var duplicateNamesInRequest = normalizedNames
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .OrderBy(x => x)
            .ToList();

        if (duplicateNamesInRequest.Count > 0)
        {
            return CreateEquipmentsBulkResult.Conflict(
                $"Duplicate equipment names in request: {string.Join(", ", duplicateNamesInRequest)}.");
        }

        var existingNames = await dbContext.Equipments
            .AsNoTracking()
            .Where(x => normalizedNames.Contains(x.Name))
            .Select(x => x.Name)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        if (existingNames.Count > 0)
        {
            return CreateEquipmentsBulkResult.Conflict(
                $"Equipments already exist: {string.Join(", ", existingNames)}.");
        }

        var entities = requests
            .Select((request, index) => MapToEntity(request, normalizedNames[index]))
            .ToList();

        dbContext.Equipments.AddRange(entities);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return CreateEquipmentsBulkResult.Conflict(
                "One or more equipment items already exist. Refresh and retry the bulk request.");
        }

        return CreateEquipmentsBulkResult.Success(entities.Count);
    }

    private static Expression<Func<Equipment, EquipmentResponse>> MapToResponseExpression()
    {
        return x => new EquipmentResponse
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            HowTo = x.HowTo,
            WeightKg = x.WeightKg,
            CreatedAtUtc = x.CreatedAtUtc
        };
    }

    private static Equipment MapToEntity(CreateEquipmentRequest request, string normalizedName)
    {
        return new Equipment
        {
            Name = normalizedName,
            Description = StorageTextNormalizer.NormalizeOptionalText(request.Description),
            HowTo = StorageTextNormalizer.NormalizeOptionalText(request.HowTo),
            WeightKg = request.WeightKg ?? 0d
        };
    }
}
