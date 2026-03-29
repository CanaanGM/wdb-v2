using Api.Features.Equipments.Contracts;

namespace Api.Features.Equipments.Services;

public interface IEquipmentsService
{
    Task<List<EquipmentResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<List<EquipmentResponse>> SearchAsync(string searchTerm, CancellationToken cancellationToken);

    Task<EquipmentResponse?> GetByNameAsync(string name, CancellationToken cancellationToken);

    Task<CreateEquipmentResult> CreateAsync(CreateEquipmentRequest request, CancellationToken cancellationToken);

    Task<CreateEquipmentsBulkResult> CreateBulkAsync(
        List<CreateEquipmentRequest> requests,
        CancellationToken cancellationToken);
}
