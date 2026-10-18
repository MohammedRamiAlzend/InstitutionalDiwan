using InstitutionalDiwan.Domain.Entities.Relations;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>Seeds the document relation type lookup (Order 56, every environment).</summary>
public sealed class DocumentRelationTypeSeeder : LookupSeederBase<DocumentRelationType>
{
    public override int Order => 56;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (DocumentRelationType.ReplyToId, "Reply to", "ردّ على"),
        (DocumentRelationType.RelatedId, "Related", "مرتبط"),
        (DocumentRelationType.SupersedesId, "Supersedes", "يلغي"),
        (DocumentRelationType.DuplicateOfId, "Duplicate of", "نسخة عن"),
    };

    protected override DocumentRelationType BuildItem(int id, string desc, string descAr)
    {
        var (isSymmetric, preventCycles) = id switch
        {
            DocumentRelationType.RelatedId => (true, false),
            _ => (false, true),
        };

        return DocumentRelationType.Create(id, desc, descAr, isSymmetric, preventCycles).Value;
    }
}

/// <summary>Seeds the relation action type lookup (Order 57, every environment).</summary>
public sealed class RelationActionTypeSeeder : LookupSeederBase<RelationActionType>
{
    public override int Order => 57;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (RelationActionType.CreatedId, "Created", "إنشاء"),
        (RelationActionType.UpdatedId, "Updated", "تحديث"),
        (RelationActionType.RemovedId, "Removed", "حذف"),
    };

    protected override RelationActionType BuildItem(int id, string desc, string descAr) =>
        RelationActionType.Create(id, desc, descAr).Value;
}
