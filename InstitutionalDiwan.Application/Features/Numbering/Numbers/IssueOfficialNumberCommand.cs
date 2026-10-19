using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

/// <summary>
/// Issues the next gap-free official number for a DRAFT document and registers it. The number is
/// rendered from the registry's template; a document that already has an active number is rejected.
/// </summary>
public sealed record IssueOfficialNumberCommand(
    Guid DocumentId,
    Guid UserId,
    int? NumberingYear = null) : ICommand<DocumentNumberResponse>;
