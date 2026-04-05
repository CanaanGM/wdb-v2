using Api.Features.Measurements.Contracts;

namespace Api.Features.Measurements.Services;

public interface IMeasurementsService
{
    Task<List<MeasurementResponse>> GetAllAsync(int userId, CancellationToken cancellationToken);

    Task<MeasurementResponse?> GetByIdAsync(int userId, int measurementId, CancellationToken cancellationToken);

    Task<MeasurementOperationResult<MeasurementResponse>> CreateAsync(
        int userId,
        MeasurementUpsertRequest request,
        CancellationToken cancellationToken);

    Task<MeasurementOperationResult<MeasurementResponse>> UpdateAsync(
        int userId,
        int measurementId,
        MeasurementUpsertRequest request,
        CancellationToken cancellationToken);

    Task<MeasurementOperationResult> DeleteAsync(int userId, int measurementId, CancellationToken cancellationToken);
}
