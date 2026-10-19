using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

/// <summary>
/// Corrects a document's number: allocates a fresh sequence, marks the old number REPLACED with a
/// link to its replacement, and re-points the document at the new number. Returns the new number.
/// </summary>
public sealed record ReplaceOfficialNumberCommand(
    Guid DocumentId,
    Guid DocumentNumberId,
    Guid UserId,
    string Reason,
    int? NumberingYear = null) : ICommand<DocumentNumberResponse>;
