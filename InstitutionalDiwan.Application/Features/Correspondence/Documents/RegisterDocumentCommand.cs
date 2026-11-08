using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Transitions a DRAFT document to REGISTERED. The central system number (a GUID) is issued by the
/// server and the numbering year defaults to the document's year or the current year
/// (<paramref name="NumberingYear"/> is an internal override). When
/// <paramref name="DepartmentRegistryNumberText"/> is supplied the number and
/// <paramref name="DepartmentRegistryDate"/> are also written into the caller's department register,
/// and an optional <paramref name="Movement"/> sends the document on at once; all in one transaction.
/// </summary>
public sealed record RegisterDocumentCommand(
    Guid DocumentId,
    Guid UserId,
    int? NumberingYear = null,
    string? DepartmentRegistryNumberText = null,
    DateTime? DepartmentRegistryDate = null,
    CreateDocumentMovementInput? Movement = null,
    Guid? DepartmentRegistryId = null) : ICommand<DocumentResponse>;
