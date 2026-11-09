using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>Read model returned by the document endpoints.</summary>
public sealed record DocumentResponse(
    Guid DocumentId,
    string Title,
    string? SubjectSummary,
    int DocumentTypeId,
    int? DirectionId,
    int? ConfidentialityLevelId,
    int? PriorityLevelId,
    int StatusId,
    Guid OwningDepartmentId,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    string? OfficialNumberText = null,
    DateTime? RegistrationDate = null,
    IReadOnlyDictionary<string, string?>? FieldValues = null)
{
    public static DocumentResponse From(Document document) => new(
        document.DocumentId,
        document.DisplayName,
        document.SubjectSummary,
        document.DocumentTypeId,
        document.DirectionId,
        document.ConfidentialityLevelId,
        document.PriorityLevelId,
        document.CurrentStatusId,
        document.OwningDepartmentId,
        document.CreatedByUserId,
        document.CreatedAt,
        document.OfficialNumberText,
        document.RegistrationDate);
}
