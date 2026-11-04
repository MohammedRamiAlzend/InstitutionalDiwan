using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal static class DocumentViewAccessQuery
{
    /// <summary>
    /// The <c>VIEW</c> rule of <c>DocumentAccessEvaluator</c> as SQL: authorship, a valid grant, or, for
    /// documents that confidentiality does not restrict, organizational scope or a department on the
    /// dispatched current step of an active movement. Persistence tests hold the two rules equal.
    /// </summary>
    public static IQueryable<Document> WhereViewableBy(
        this IQueryable<Document> documents,
        DiwanDbContext context,
        DocumentViewAccess access)
    {
        var callerId = access.CallerUserId;
        var asOf = access.AsOfUtc;
        var validGrants = context.DocumentAccessGrants.Where(grant =>
            grant.UserId == callerId &&
            !grant.IsRevoked &&
            (grant.ExpiresAt == null || grant.ExpiresAt > asOf));

        const int secret = DocumentConfidentialityLevel.SecretId;
        const int topSecret = DocumentConfidentialityLevel.TopSecretId;

        if (access.AccessibleDepartmentIds is not { } departmentIds)
        {
            return documents.Where(document =>
                document.CreatedByUserId == callerId ||
                validGrants.Any(grant => grant.DocumentId == document.DocumentId) ||
                (document.ConfidentialityLevelId != secret && document.ConfidentialityLevelId != topSecret));
        }

        var participatingMovements = context.DocumentMovements.Where(movement =>
            movement.StatusId != DocumentMovement.StatusClosed &&
            movement.StatusId != DocumentMovement.StatusCanceled &&
            movement.Steps.Any(step =>
                step.StepNo == movement.CurrentStepNo &&
                (((step.StepStatusId == MovementStepStatus.SentId ||
                   step.StepStatusId == MovementStepStatus.ReceivedId ||
                   step.StepStatusId == MovementStepStatus.CompletedId) &&
                  (departmentIds.Contains(step.FromDepartmentId) ||
                   departmentIds.Contains(step.ToDepartmentId))) ||
                 // Bounced back (returned or rejected): only the department that sent it.
                 ((step.StepStatusId == MovementStepStatus.ReturnedId ||
                   step.StepStatusId == MovementStepStatus.RejectedId) &&
                  departmentIds.Contains(step.FromDepartmentId)))));

        return documents.Where(document =>
            document.CreatedByUserId == callerId ||
            validGrants.Any(grant => grant.DocumentId == document.DocumentId) ||
            (document.ConfidentialityLevelId != secret &&
             document.ConfidentialityLevelId != topSecret &&
             (departmentIds.Contains(document.OwningDepartmentId) ||
              participatingMovements.Any(movement => movement.DocumentId == document.DocumentId))));
    }
}
