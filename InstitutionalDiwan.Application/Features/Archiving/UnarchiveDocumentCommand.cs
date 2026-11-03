using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Correspondence.Documents;

namespace InstitutionalDiwan.Application.Features.Archiving;

/// <summary>
/// Restores an archived document to the status it held before archiving. The reason is required
/// because leaving the archive is an exception that has to be justified.
/// </summary>
public sealed record UnarchiveDocumentCommand(
    Guid DocumentId,
    Guid UserId,
    string Reason) : ICommand<DocumentResponse>;
