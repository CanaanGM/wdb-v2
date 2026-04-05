using Api.Application.Contracts.Querying;
using Api.Features.Plans.Contracts;

namespace Api.Features.Plans.Services;

public interface IPlansService
{
    Task<PagedResponse<PlanTemplateResponse>> SearchPlansAsync(SearchPlansRequest request, CancellationToken cancellationToken);

    Task<PlanTemplateDetailsResponse?> GetPlanByIdAsync(int planId, CancellationToken cancellationToken);

    Task<PlanOperationResult<PlanTemplateDetailsResponse>> CreatePlanAsync(CreatePlanTemplateRequest request, CancellationToken cancellationToken);

    Task<PlanOperationResult<int>> CreatePlansBulkAsync(IReadOnlyList<CreatePlanTemplateRequest> requests, CancellationToken cancellationToken);

    Task<PlanOperationResult<PlanTemplateDetailsResponse>> UpdatePlanAsync(int planId, UpdatePlanTemplateRequest request, CancellationToken cancellationToken);

    Task<PlanOperationResult<int>> CreatePlanDayExercisesBulkAsync(
        int planId,
        int weekNumber,
        int dayNumber,
        IReadOnlyList<PlanDayExerciseRequest> requests,
        CancellationToken cancellationToken);

    Task<PlanOperationResult<UserPlanEnrollmentResponse>> EnrollAsync(
        int userId,
        int planId,
        EnrollInPlanRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<UserPlanEnrollmentResponse>> SearchEnrollmentsAsync(
        int userId,
        SearchUserPlanEnrollmentsRequest request,
        CancellationToken cancellationToken);

    Task<List<UserPlanAgendaDayResponse>> GetAgendaAsync(
        int userId,
        GetUserPlanAgendaRequest request,
        CancellationToken cancellationToken);

    Task<PlanOperationResult<UserPlanDayExecutionResponse>> CompleteDayAsync(
        int userId,
        int enrollmentId,
        CompletePlanDayRequest request,
        CancellationToken cancellationToken);

    Task<PlanOperationResult<UserPlanDayExecutionResponse>> SkipDayAsync(
        int userId,
        int enrollmentId,
        SkipPlanDayRequest request,
        CancellationToken cancellationToken);

    Task<PlanOperationResult<UserPlanExerciseExecutionResponse>> CompleteExerciseAsync(
        int userId,
        int enrollmentId,
        CompletePlanExerciseRequest request,
        CancellationToken cancellationToken);

    Task<PlanOperationResult> CancelEnrollmentAsync(int userId, int enrollmentId, CancellationToken cancellationToken);
}
