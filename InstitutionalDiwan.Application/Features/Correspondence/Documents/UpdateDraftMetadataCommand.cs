using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Edits a draft's core metadata. Only DRAFT documents are editable through this command; once
/// registered, changes must go through a correction workflow.
/// </summary>
public sealed record UpdateDraftMetadataCommand(
    Guid DocumentId,
    string Title,
    string? SubjectSummary = null,
    int? ConfidentialityLevelId = null,
    int? PriorityLevelId = null) : ICommand<DocumentResponse>;
