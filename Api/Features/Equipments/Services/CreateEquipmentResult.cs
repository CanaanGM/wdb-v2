using Api.Features.Equipments.Contracts;

namespace Api.Features.Equipments.Services;

public enum CreateEquipmentResultType
{
    Success = 0,
    ValidationError = 1,
    Conflict = 2
}

public sealed class CreateEquipmentResult
{
    private CreateEquipmentResult(
        CreateEquipmentResultType resultType,
        EquipmentResponse? equipment = null,
        string? error = null)
    {
        ResultType = resultType;
        Equipment = equipment;
        Error = error;
    }

    public CreateEquipmentResultType ResultType { get; }

    public EquipmentResponse? Equipment { get; }

    public string? Error { get; }

    public static CreateEquipmentResult Success(EquipmentResponse equipment) =>
        new(CreateEquipmentResultType.Success, equipment);

    public static CreateEquipmentResult ValidationError(string error) =>
        new(CreateEquipmentResultType.ValidationError, error: error);

    public static CreateEquipmentResult Conflict(string error) =>
        new(CreateEquipmentResultType.Conflict, error: error);
}
