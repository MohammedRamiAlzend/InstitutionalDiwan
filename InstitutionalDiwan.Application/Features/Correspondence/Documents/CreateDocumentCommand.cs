using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Creates a new DRAFT document owned by the caller's department. <c>UserId</c> is derived from
/// the token by the endpoint, never from the request body.
/// </summary>
public sealed record CreateDocumentCommand(
    Guid UserId,
    string Title,
    string DocumentTypeCode,
    string Direction,
    string? SubjectSummary = null,
    string? ConfidentialityLevel = null,
    string? PriorityLevel = null) : ICommand<DocumentResponse>;
