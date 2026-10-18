using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
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
    public string RoleCode { get; private set; } = null!;
    public string TargetType { get; private set; } = null!;
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
        string? roleCode,
        ParticipantTarget target,
        bool isPrimary = false,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
            return Result.Failure<DocumentParticipant>(DomainErrors.Correspondence.ParticipantRoleEmpty);

        var participant = new DocumentParticipant
        {
            ParticipantId = Guid.NewGuid(),
            DocumentId = documentId,
            RoleCode = roleCode.Trim().ToUpperInvariant(),
            TargetType = target.Kind.ToString().ToUpperInvariant(),
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
