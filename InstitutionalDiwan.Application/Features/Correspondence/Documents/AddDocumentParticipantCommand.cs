using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Attaches a participant to a document. Exactly one target must be supplied: an external
/// organization, an internal department, or a person name. A document can hold at most one external
/// organization participant.
/// </summary>
public sealed record AddDocumentParticipantCommand(
    Guid DocumentId,
    int RoleId,
    Guid? ExternalOrganizationId = null,
    Guid? InternalDepartmentId = null,
    string? PersonName = null,
    bool IsPrimary = false,
    string? Notes = null) : ICommand<DocumentParticipantResponse>;
