using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Who may act on a movement. Custody follows the caller's department on the current step, not their
/// access to the document, so a department can process a SECRET document it is not allowed to read.
/// </summary>
internal static class MovementCustody
{
    /// <summary>The caller's scope must cover the department the current step leaves.</summary>
    public static Task<Result> EnsureSenderAsync(
        DocumentMovement movement,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        CancellationToken cancellationToken) =>
        EnsureCoversAsync(movement, departmentScopeEvaluator, sender: true, receiver: false, cancellationToken);

    /// <summary>The caller's scope must cover the department the current step is addressed to.</summary>
    public static Task<Result> EnsureReceiverAsync(
        DocumentMovement movement,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        CancellationToken cancellationToken) =>
        EnsureCoversAsync(movement, departmentScopeEvaluator, sender: false, receiver: true, cancellationToken);

    /// <summary>The caller's scope must cover either side of the current step.</summary>
    public static Task<Result> EnsureEitherSideAsync(
        DocumentMovement movement,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        CancellationToken cancellationToken) =>
        EnsureCoversAsync(movement, departmentScopeEvaluator, sender: true, receiver: true, cancellationToken);

    private static async Task<Result> EnsureCoversAsync(
        DocumentMovement movement,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        bool sender,
        bool receiver,
        CancellationToken cancellationToken)
    {
        var step = movement.Steps.FirstOrDefault(candidate => candidate.StepNo == movement.CurrentStepNo);
        if (step is null)
            return Result.Failure(DomainErrors.Movements.NoActiveStep);

        if (sender && await departmentScopeEvaluator.CanAccessDepartmentAsync(step.FromDepartmentId, cancellationToken))
            return Result.Success();

        if (receiver && await departmentScopeEvaluator.CanAccessDepartmentAsync(step.ToDepartmentId, cancellationToken))
            return Result.Success();

        return Result.Failure(DomainErrors.Movements.DepartmentOutOfScope);
    }
}
