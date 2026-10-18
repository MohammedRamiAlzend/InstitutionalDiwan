using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Creates a new document owned by the caller's department with the supplied official number.
/// <c>UserId</c> is derived from the token by the endpoint, never from the request body.
/// The document is persisted as REGISTERED when the number+year pair is free, or rejected with a
/// Conflict when it is already taken.
/// </summary>
public sealed record CreateDocumentCommand(
    Guid UserId,
    string Title,
    string DocumentTypeCode,
    string Direction,
    string OfficialNumberText,
    int NumberingYear,
    string? SubjectSummary = null,
    string? ConfidentialityLevel = null,
    string? PriorityLevel = null) : ICommand<DocumentResponse>;
