using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Closing a document (archive, unarchive, cancel) belongs to the department that created it. Other
/// departments that hold or held the document keep their own records but cannot close it. The rule
/// follows the caller's department scope, so the owning department's sub-departments (when the
/// assignment includes them) and institution-wide administrators pass.
/// </summary>
internal static class OwningDepartmentGuard
{
    public static async Task<Result> EnsureOwningDepartmentAsync(
        Document document,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        CancellationToken cancellationToken) =>
        await departmentScopeEvaluator.CanAccessDepartmentAsync(document.OwningDepartmentId, cancellationToken)
            ? Result.Success()
            : Result.Failure(DomainErrors.Correspondence.OnlyOwningDepartmentCanClose);
}
