using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Decisions;

/// <summary>
/// A decision kind an administrator defines: whether it can be appealed, what it does, and, for a
/// referral, which department it points at. Issued decisions keep their own snapshot, so editing a
/// type never rewrites a decision already on a file. A type that has been used is deactivated, not
/// deleted.
/// </summary>
public sealed class DecisionType : AggregateRoot<Guid>
{
    public const int MaxNameLength = 200;

    private readonly List<DecisionTypeDocumentType> _documentTypes = new();

    public Guid DecisionTypeId
    {
        get => Id;
        private set => Id = value;
    }

    public string NameAr { get; private set; } = null!;
    public bool IsAppealable { get; private set; }
    public int EffectId { get; private set; }
    public Guid? DestinationDepartmentId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<DecisionTypeDocumentType> DocumentTypes => _documentTypes;

    private DecisionType()
    {
    }

    public bool AllowsDocumentType(int documentTypeId) =>
        _documentTypes.Any(link => link.DocumentTypeId == documentTypeId);

    public static Result<DecisionType> Create(
        string? nameAr,
        bool isAppealable,
        int effectId,
        Guid? destinationDepartmentId,
        IReadOnlyCollection<int> documentTypeIds,
        bool isActive = true,
        DateTime? utcNow = null,
        Guid? decisionTypeId = null)
    {
        var shape = ValidateShape(nameAr, effectId, destinationDepartmentId, requireDestination: isActive);
        if (shape.IsFailure)
            return Result.Failure<DecisionType>(shape.Error);

        var type = new DecisionType
        {
            DecisionTypeId = decisionTypeId is null || decisionTypeId == Guid.Empty
                ? Guid.NewGuid()
                : decisionTypeId.Value,
            NameAr = nameAr!.Trim(),
            IsAppealable = isAppealable,
            EffectId = effectId,
            DestinationDepartmentId = NormalizeDestination(effectId, destinationDepartmentId),
            IsActive = isActive,
            CreatedAt = utcNow ?? DateTime.UtcNow,
        };
        type.ReplaceDocumentTypes(documentTypeIds);
        return Result.Success(type);
    }

    public Result Update(
        string? nameAr,
        bool isAppealable,
        int effectId,
        Guid? destinationDepartmentId,
        IReadOnlyCollection<int> documentTypeIds,
        DateTime? utcNow = null)
    {
        var shape = ValidateShape(nameAr, effectId, destinationDepartmentId, requireDestination: IsActive);
        if (shape.IsFailure)
            return shape;

        NameAr = nameAr!.Trim();
        IsAppealable = isAppealable;
        EffectId = effectId;
        DestinationDepartmentId = NormalizeDestination(effectId, destinationDepartmentId);
        ReplaceDocumentTypes(documentTypeIds);
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>A referral cannot be activated until it names the department the file should go to.</summary>
    public Result Activate(DateTime? utcNow = null)
    {
        if (EffectId == DecisionEffect.ReferId && DestinationDepartmentId is null)
            return Result.Failure(DomainErrors.Decisions.TypeDestinationRequired);

        if (IsActive)
            return Result.Success();

        IsActive = true;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result Deactivate(DateTime? utcNow = null)
    {
        if (!IsActive)
            return Result.Success();

        IsActive = false;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    private void ReplaceDocumentTypes(IReadOnlyCollection<int> documentTypeIds)
    {
        _documentTypes.Clear();
        foreach (var documentTypeId in documentTypeIds.Where(id => id > 0).Distinct())
            _documentTypes.Add(new DecisionTypeDocumentType(DecisionTypeId, documentTypeId));
    }

    private static Guid? NormalizeDestination(int effectId, Guid? destinationDepartmentId)
    {
        if (effectId != DecisionEffect.ReferId || destinationDepartmentId is null || destinationDepartmentId == Guid.Empty)
            return null;

        return destinationDepartmentId;
    }

    private static Result ValidateShape(
        string? nameAr,
        int effectId,
        Guid? destinationDepartmentId,
        bool requireDestination)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure(DomainErrors.Decisions.NameEmpty);

        if (nameAr.Trim().Length > MaxNameLength)
            return Result.Failure(DomainErrors.Decisions.NameTooLong);

        if (!DecisionEffect.IsKnown(effectId))
            return Result.Failure(DomainErrors.Decisions.EffectInvalid);

        var destination = destinationDepartmentId is null || destinationDepartmentId == Guid.Empty
            ? null
            : destinationDepartmentId;

        if (effectId == DecisionEffect.ReferId)
        {
            if (requireDestination && destination is null)
                return Result.Failure(DomainErrors.Decisions.TypeDestinationRequired);
        }
        else if (destination is not null)
        {
            return Result.Failure(DomainErrors.Decisions.TypeDestinationNotAllowed);
        }

        return Result.Success();
    }
}
