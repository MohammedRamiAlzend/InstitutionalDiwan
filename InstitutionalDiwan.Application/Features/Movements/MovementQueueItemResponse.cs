using InstitutionalDiwan.Domain.Entities.Movements;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// One row of a department's inbox or outbox: the document as the department sees it, together
/// with the movement and the current step that carry it.
/// </summary>
/// <param name="MovementId">Movement unique identifier.</param>
/// <param name="DocumentId">Document unique identifier.</param>
/// <param name="DocumentTitle">Document title.</param>
/// <param name="OfficialNumberText">Central official number of the document, when it has one.</param>
/// <param name="MovementTypeId">Movement type id.</param>
/// <param name="MovementStatusId">Movement status id.</param>
/// <param name="StepNo">Number of the current step.</param>
/// <param name="StepStatusId">Status id of the current step.</param>
/// <param name="FromDepartmentId">Department the current step leaves.</param>
/// <param name="ToDepartmentId">Department the current step is addressed to.</param>
/// <param name="AssignedToUserId">User the current step is assigned to, when any.</param>
/// <param name="Instructions">Instructions carried by the current step.</param>
/// <param name="DueDate">Due date of the current step.</param>
/// <param name="CreatedAt">Moment the movement was opened.</param>
/// <param name="SentAt">Moment the current step was sent.</param>
/// <param name="ReceivedAt">Moment the current step was received.</param>
/// <param name="CompletedAt">Moment the current step was completed.</param>
public sealed record MovementQueueItemResponse(
    Guid MovementId,
    Guid DocumentId,
    string DocumentTitle,
    string? OfficialNumberText,
    int MovementTypeId,
    int MovementStatusId,
    int StepNo,
    int StepStatusId,
    Guid FromDepartmentId,
    Guid ToDepartmentId,
    Guid? AssignedToUserId,
    string? Instructions,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? SentAt,
    DateTime? ReceivedAt,
    DateTime? CompletedAt)
{
    /// <summary>
    /// Builds the row from a movement and its current step. The document's title and official
    /// number are resolved by the caller (a batch lookup), because a movement does not carry them.
    /// </summary>
    public static MovementQueueItemResponse From(
        DocumentMovement movement,
        string documentTitle,
        string? officialNumberText)
    {
        var step = movement.Steps.FirstOrDefault(candidate => candidate.StepNo == movement.CurrentStepNo);

        return new MovementQueueItemResponse(
            movement.MovementId,
            movement.DocumentId,
            documentTitle,
            officialNumberText,
            movement.MovementTypeId,
            movement.StatusId,
            movement.CurrentStepNo,
            step?.StepStatusId ?? MovementStepStatus.PendingId,
            step?.FromDepartmentId ?? movement.SourceDepartmentId,
            step?.ToDepartmentId ?? movement.DestinationDepartmentId,
            step?.AssignedToUserId ?? movement.AssignedToUserId,
            step?.Instructions ?? movement.Instructions,
            step?.DueDate ?? movement.DueDate,
            movement.CreatedAt,
            step?.SentAt,
            step?.ReceivedAt,
            step?.CompletedAt);
    }
}
