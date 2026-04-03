namespace Api.Features.Equipments.Services;

public enum DeleteEquipmentResultType
{
    Success = 0,
    NotFound = 1,
    ValidationError = 2
}

public sealed class DeleteEquipmentResult
{
    private DeleteEquipmentResult(DeleteEquipmentResultType resultType, string? error = null)
    {
        ResultType = resultType;
        Error = error;
    }

    public DeleteEquipmentResultType ResultType { get; }

    public string? Error { get; }

    public static DeleteEquipmentResult Success() => new(DeleteEquipmentResultType.Success);

    public static DeleteEquipmentResult NotFound(string error) =>
        new(DeleteEquipmentResultType.NotFound, error);

    public static DeleteEquipmentResult ValidationError(string error) =>
        new(DeleteEquipmentResultType.ValidationError, error);
}
