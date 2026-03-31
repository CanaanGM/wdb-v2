namespace Api.Features.UserExerciseStats.Options;

public sealed class UserExerciseStatsMaintenanceOptions
{
    public const string SectionName = "UserExerciseStats:Maintenance";

    public bool RecomputeAllOnStartup { get; set; }
}
