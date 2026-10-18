using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>Read model returned by the document endpoints.</summary>
public sealed record DocumentResponse(
    Guid DocumentId,
    string Title,
    string? SubjectSummary,
    string DocumentTypeCode,
    string Direction,
    string ConfidentialityLevel,
    string PriorityLevel,
    string Status,
    Guid OwningDepartmentId,
    Guid CreatedByUserId,
    DateTime DocumentDate,
    DateTime CreatedAt,
    string? OfficialNumberText = null,
    DateTime? RegistrationDate = null)
{
    public static DocumentResponse From(Document document) => new(
        document.DocumentId,
        document.Title,
        document.SubjectSummary,
        document.DocumentTypeCode,
        document.Direction,
        document.ConfidentialityLevel,
        document.PriorityLevel,
        document.CurrentStatusCode,
        document.OwningDepartmentId,
        document.CreatedByUserId,
        document.DocumentDate ?? document.CreatedAt,
        document.CreatedAt,
        document.OfficialNumberText,
        document.RegistrationDate);
}
