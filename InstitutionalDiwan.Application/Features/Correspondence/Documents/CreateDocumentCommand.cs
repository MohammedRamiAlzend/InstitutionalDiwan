using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Creates a new document owned by the caller's department with the supplied official number.
/// <c>UserId</c> is derived from the token by the endpoint, never from the request body.
/// The document is persisted as REGISTERED when the number+year pair is free, or rejected with a
/// Conflict when it is already taken.
/// <c>DocumentTypeId</c>, <c>DirectionId</c>, <c>ConfidentialityLevelId</c>, and
/// <c>PriorityLevelId</c> reference the corresponding seeded lookup tables.
/// </summary>
public sealed record CreateDocumentCommand(
    Guid UserId,
    string Title,
    int DocumentTypeId,
    int DirectionId,
    string OfficialNumberText,
    int NumberingYear,
    string? SubjectSummary = null,
    int? ConfidentialityLevelId = null,
    int? PriorityLevelId = null) : ICommand<DocumentResponse>;
