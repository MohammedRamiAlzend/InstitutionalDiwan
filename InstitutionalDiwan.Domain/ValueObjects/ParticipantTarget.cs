namespace InstitutionalDiwan.Domain.ValueObjects;

public enum ParticipantTargetKind
{
    External = 1,
    Internal = 2,
    Person = 3
}

public sealed class ParticipantTarget : ValueObject
{
    public ParticipantTargetKind Kind { get; }
    public Guid? ExternalOrganizationId { get; }
    public Guid? InternalDepartmentId { get; }
    public string? PersonName { get; }

    private ParticipantTarget(
        ParticipantTargetKind kind,
        Guid? externalOrganizationId,
        Guid? internalDepartmentId,
        string? personName)
    {
        Kind = kind;
        ExternalOrganizationId = externalOrganizationId;
        InternalDepartmentId = internalDepartmentId;
        PersonName = personName;
    }

    public static Result<ParticipantTarget> External(Guid externalOrganizationId)
    {
        if (externalOrganizationId == Guid.Empty)
            return Result.Failure<ParticipantTarget>(DomainErrors.Correspondence.ParticipantTargetInvalid);

        return Result.Success(new ParticipantTarget(ParticipantTargetKind.External, externalOrganizationId, null, null));
    }

    public static Result<ParticipantTarget> Internal(Guid internalDepartmentId)
    {
        if (internalDepartmentId == Guid.Empty)
            return Result.Failure<ParticipantTarget>(DomainErrors.Correspondence.ParticipantTargetInvalid);

        return Result.Success(new ParticipantTarget(ParticipantTargetKind.Internal, null, internalDepartmentId, null));
    }

    public static Result<ParticipantTarget> Person(string? personName)
    {
        if (string.IsNullOrWhiteSpace(personName))
            return Result.Failure<ParticipantTarget>(DomainErrors.Correspondence.ParticipantTargetInvalid);

        return Result.Success(new ParticipantTarget(ParticipantTargetKind.Person, null, null, personName.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kind;
        yield return ExternalOrganizationId;
        yield return InternalDepartmentId;
        yield return PersonName;
    }
}
