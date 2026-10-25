using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Guards the step-scoped routes: when a caller names a step, it must be the movement's current
/// step. The step id is optional so the original movement-scoped routes keep working.
/// </summary>
internal static class MovementStepGuard
{
    public static Result EnsureCurrent(DocumentMovement movement, Guid? stepId)
    {
        if (stepId is not { } requestedStepId)
            return Result.Success();

        var currentStep = movement.Steps
            .FirstOrDefault(step => step.StepNo == movement.CurrentStepNo);

        return currentStep?.StepId == requestedStepId
            ? Result.Success()
            : Result.Failure(DomainErrors.Movements.StepNotCurrent);
    }
}
