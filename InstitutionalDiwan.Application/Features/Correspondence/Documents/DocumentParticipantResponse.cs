using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>One party on a document: an external organization, an internal department, or a person.</summary>
public sealed record DocumentParticipantResponse(
    Guid ParticipantId,
    int RoleId,
    int TargetTypeId,
    Guid? InternalDepartmentId,
    Guid? ExternalOrganizationId,
    string? PersonName,
    bool IsPrimary,
    string? Notes,
    DateTime CreatedAt)
{
    public static DocumentParticipantResponse From(DocumentParticipant participant) => new(
        participant.ParticipantId,
        participant.RoleId,
        participant.TargetTypeId,
        participant.InternalDepartmentId,
        participant.ExternalOrganizationId,
        participant.PersonName,
        participant.IsPrimary,
        participant.Notes,
        participant.CreatedAt);
}
