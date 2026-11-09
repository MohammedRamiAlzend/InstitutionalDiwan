using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Persons;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Creates a document in DRAFT status without consuming an official number. The document is owned
/// by the caller's primary department and only becomes official through <c>RegisterDocumentCommand</c>.
/// </summary>
public sealed record CreateDraftDocumentCommand(
    Guid UserId,
    string? Title,
    int DocumentTypeId,
    string? SubjectSummary = null,
    int? DirectionId = null,
    int? ConfidentialityLevelId = null,
    int? PriorityLevelId = null,
    IReadOnlyList<DocumentFieldValueInput>? FieldValues = null,
    IReadOnlyList<DocumentPersonInput>? Persons = null) : ICommand<DocumentResponse>;
