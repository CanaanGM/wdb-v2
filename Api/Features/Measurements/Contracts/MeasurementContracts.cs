using System.ComponentModel.DataAnnotations;

namespace Api.Features.Measurements.Contracts;

public sealed class MeasurementUpsertRequest
{
    [Range(0d, double.MaxValue)]
    public double? Hip { get; set; }

    [Range(0d, double.MaxValue)]
    public double? Chest { get; set; }

    [Range(0d, double.MaxValue)]
    public double? WaistUnderBelly { get; set; }

    [Range(0d, double.MaxValue)]
    public double? WaistOnBelly { get; set; }

    [Range(0d, double.MaxValue)]
    public double? LeftThigh { get; set; }

    [Range(0d, double.MaxValue)]
    public double? RightThigh { get; set; }

    [Range(0d, double.MaxValue)]
    public double? LeftCalf { get; set; }

    [Range(0d, double.MaxValue)]
    public double? RightCalf { get; set; }

    [Range(0d, double.MaxValue)]
    public double? LeftUpperArm { get; set; }

    [Range(0d, double.MaxValue)]
    public double? LeftForearm { get; set; }

    [Range(0d, double.MaxValue)]
    public double? RightUpperArm { get; set; }

    [Range(0d, double.MaxValue)]
    public double? RightForearm { get; set; }

    [Range(0d, double.MaxValue)]
    public double? Neck { get; set; }

    [Range(0d, double.MaxValue)]
    public double? Minerals { get; set; }

    [Range(0d, double.MaxValue)]
    public double? Protein { get; set; }

    [Range(0d, double.MaxValue)]
    public double? TotalBodyWater { get; set; }

    [Range(0d, double.MaxValue)]
    public double? BodyFatMass { get; set; }

    [Range(0d, double.MaxValue)]
    public double? BodyWeight { get; set; }

    [Range(0d, 100d)]
    public double? BodyFatPercentage { get; set; }

    [Range(0d, double.MaxValue)]
    public double? SkeletalMuscleMass { get; set; }

    [Range(0d, double.MaxValue)]
    public double? InBodyScore { get; set; }

    [Range(0d, double.MaxValue)]
    public double? BodyMassIndex { get; set; }

    [Range(0, int.MaxValue)]
    public int? BasalMetabolicRate { get; set; }

    [Range(0, int.MaxValue)]
    public int? VisceralFatLevel { get; set; }
}

public sealed class MeasurementResponse
{
    public int Id { get; set; }

    public double? Hip { get; set; }

    public double? Chest { get; set; }

    public double? WaistUnderBelly { get; set; }

    public double? WaistOnBelly { get; set; }

    public double? LeftThigh { get; set; }

    public double? RightThigh { get; set; }

    public double? LeftCalf { get; set; }

    public double? RightCalf { get; set; }

    public double? LeftUpperArm { get; set; }

    public double? LeftForearm { get; set; }

    public double? RightUpperArm { get; set; }

    public double? RightForearm { get; set; }

    public double? Neck { get; set; }

    public double? Minerals { get; set; }

    public double? Protein { get; set; }

    public double? TotalBodyWater { get; set; }

    public double? BodyFatMass { get; set; }

    public double? BodyWeight { get; set; }

    public double? BodyFatPercentage { get; set; }

    public double? SkeletalMuscleMass { get; set; }

    public double? InBodyScore { get; set; }

    public double? BodyMassIndex { get; set; }

    public int? BasalMetabolicRate { get; set; }

    public int? VisceralFatLevel { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
