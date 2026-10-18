using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Relations;

/// <summary>
/// Reference table describing how two documents are linked. Replaces the former string
/// <c>DocumentRelation.RelationTypeCode</c>. Seeded by <c>DocumentRelationTypeSeeder</c>.
/// </summary>
public sealed class DocumentRelationType : LookupEntity
{
    public const int ReplyToId = 1;
    public const int RelatedId = 2;
    public const int SupersedesId = 3;
    public const int DuplicateOfId = 4;

    /// <summary>Whether the link is symmetric (A→B implies B→A).</summary>
    public bool IsSymmetric { get; private set; }

    /// <summary>Whether linking through this type must not create a cycle.</summary>
    public bool PreventCycles { get; private set; } = true;

    private DocumentRelationType()
    {
    }

    private DocumentRelationType(int id, string desc, string descAr, bool isSymmetric, bool preventCycles)
        : base(id, desc, descAr)
    {
        IsSymmetric = isSymmetric;
        PreventCycles = preventCycles;
    }

    public static Result<DocumentRelationType> Create(
        int id,
        string? desc,
        string? descAr,
        bool isSymmetric = false,
        bool preventCycles = true)
    {
        if (id <= 0)
            return Result.Failure<DocumentRelationType>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<DocumentRelationType>(DomainErrors.Relations.RelationTypeEmpty);

        return Result.Success(new DocumentRelationType(id, desc, descAr, isSymmetric, preventCycles));
    }

    /// <summary>Orders the endpoints so symmetric relations always store the same source/target pair.</summary>
    public (Guid SourceId, Guid TargetId) NormalizeEndpoints(Guid sourceDocumentId, Guid targetDocumentId)
    {
        if (!IsSymmetric)
            return (sourceDocumentId, targetDocumentId);

        return sourceDocumentId.CompareTo(targetDocumentId) <= 0
            ? (sourceDocumentId, targetDocumentId)
            : (targetDocumentId, sourceDocumentId);
    }
}

/// <summary>
/// Reference table of the actions recorded against a document relation. Replaces the former string
/// <c>DocumentRelationEvent.ActionType</c>. Seeded by <c>RelationActionTypeSeeder</c>.
/// </summary>
public sealed class RelationActionType : LookupEntity
{
    public const int CreatedId = 1;
    public const int UpdatedId = 2;
    public const int RemovedId = 3;

    private RelationActionType()
    {
    }

    private RelationActionType(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<RelationActionType> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<RelationActionType>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<RelationActionType>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new RelationActionType(id, desc, descAr));
    }
}
