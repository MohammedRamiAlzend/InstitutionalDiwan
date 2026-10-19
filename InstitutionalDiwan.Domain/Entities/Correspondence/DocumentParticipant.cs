using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Domain.Entities.Correspondence;

public sealed class DocumentParticipant : Entity<Guid>
{
    public Guid ParticipantId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
    public Guid DocumentId { get; private set; }
    // FK → ParticipantRole.Id (InstitutionalDiwan.Domain.Entities.Correspondence.ParticipantRole)
    public int RoleId { get; private set; }
    // FK → ParticipantTargetType.Id (InstitutionalDiwan.Domain.Entities.Correspondence.ParticipantTargetType)
    public int TargetTypeId { get; private set; }
    // FK → Department.DepartmentId (InstitutionalDiwan.Domain.Entities.Organization.Department)
    public Guid? InternalDepartmentId { get; private set; }
    // FK → ExternalOrganization.ExternalOrganizationId (InstitutionalDiwan.Domain.Entities.Correspondence.ExternalOrganization)
    public Guid? ExternalOrganizationId { get; private set; }
    public string? PersonName { get; private set; }
    public bool IsPrimary { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private DocumentParticipant()
    {
    }

    internal static Result<DocumentParticipant> Create(
        Guid documentId,
        int roleId,
        ParticipantTarget target,
        bool isPrimary = false,
        string? notes = null)
    {
        if (roleId <= 0)
            return Result.Failure<DocumentParticipant>(DomainErrors.Correspondence.ParticipantRoleEmpty);

        var participant = new DocumentParticipant
        {
            ParticipantId = Guid.NewGuid(),
            DocumentId = documentId,
            RoleId = roleId,
            TargetTypeId = target.Kind switch
            {
                ParticipantTargetKind.External => ParticipantTargetType.ExternalOrganizationId,
                ParticipantTargetKind.Internal => ParticipantTargetType.InternalDepartmentId,
                _ => ParticipantTargetType.PersonId
            },
            InternalDepartmentId = target.InternalDepartmentId,
            ExternalOrganizationId = target.ExternalOrganizationId,
            PersonName = target.PersonName,
            IsPrimary = isPrimary,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(participant);
    }
}
