using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>Seeds the document participant role lookup (Order 44, every environment).</summary>
public sealed class ParticipantRoleSeeder : LookupSeederBase<ParticipantRole>
{
    public override int Order => 44;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (ParticipantRole.SenderId, "Sender", "مُرسل"),
        (ParticipantRole.ReceiverId, "Receiver", "مستلم"),
        (ParticipantRole.CcId, "CC", "نسخة"),
        (ParticipantRole.SignatoryId, "Signatory", "موقّع"),
    };

    protected override ParticipantRole BuildItem(int id, string desc, string descAr) =>
        ParticipantRole.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the participant target type lookup (Order 45, every environment).</summary>
public sealed class ParticipantTargetTypeSeeder : LookupSeederBase<ParticipantTargetType>
{
    public override int Order => 45;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (ParticipantTargetType.ExternalOrganizationId, "External organization", "جهة خارجية"),
        (ParticipantTargetType.InternalDepartmentId, "Internal department", "قسم داخلي"),
        (ParticipantTargetType.PersonId, "Person", "شخص"),
    };

    protected override ParticipantTargetType BuildItem(int id, string desc, string descAr) =>
        ParticipantTargetType.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the external organization type lookup (Order 46, every environment).</summary>
public sealed class OrganizationTypeSeeder : LookupSeederBase<OrganizationType>
{
    public override int Order => 46;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (OrganizationType.GovernmentId, "Government", "حكومي"),
        (OrganizationType.PrivateId, "Private", "خاص"),
        (OrganizationType.InternationalId, "International", "دولي"),
        (OrganizationType.IndividualId, "Individual", "فرد"),
    };

    protected override OrganizationType BuildItem(int id, string desc, string descAr) =>
        OrganizationType.Create(id, desc, descAr).Value;
}
