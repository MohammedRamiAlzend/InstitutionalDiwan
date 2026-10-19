using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>Seeds the document directions lookup (Order 55, every environment).</summary>
public sealed class DocumentDirectionSeeder : LookupSeederBase<DocumentDirection>
{
    public override int Order => 55;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (DocumentDirection.InternalId, "Internal", "داخلي"),
        (DocumentDirection.IncomingId, "Incoming", "وارد"),
        (DocumentDirection.OutgoingId, "Outgoing", "صادر"),
        (DocumentDirection.AllId, "All directions", "كل الاتجاهات"),
    };

    protected override DocumentDirection BuildItem(int id, string desc, string descAr) =>
        DocumentDirection.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the document types lookup (Order 60, every environment).</summary>
public sealed class DocumentTypeSeeder : LookupSeederBase<DocumentType>
{
    public override int Order => 60;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (DocumentType.IncomingLetterId, "Incoming letter", "كتاب وارد"),
        (DocumentType.OutgoingLetterId, "Outgoing letter", "كتاب صادر"),
        (DocumentType.InternalMemoId, "Internal memo", "مذكرة داخلية"),
        (DocumentType.CircularId, "Circular", "تعميم"),
        (DocumentType.DecisionId, "Decision", "قرار"),
    };

    protected override DocumentType BuildItem(int id, string desc, string descAr) => id switch
    {
        DocumentType.IncomingLetterId => DocumentType.Create(id, desc, descAr, DocumentDirection.IncomingId).Value,
        DocumentType.OutgoingLetterId => DocumentType.Create(id, desc, descAr, DocumentDirection.OutgoingId).Value,
        DocumentType.InternalMemoId => DocumentType.Create(id, desc, descAr, DocumentDirection.InternalId, requiresOfficialNumber: false).Value,
        _ => DocumentType.Create(id, desc, descAr, DocumentDirection.AllId).Value,
    };
}

/// <summary>Seeds the document status lookup (Order 65, every environment).</summary>
public sealed class DocumentStatusTypeSeeder : LookupSeederBase<DocumentStatusType>
{
    public override int Order => 65;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (DocumentStatusType.DraftId, "Draft", "مسودة"),
        (DocumentStatusType.RegisteredId, "Registered", "مسجل"),
        (DocumentStatusType.InMovementId, "In movement", "قيد التداول"),
        (DocumentStatusType.ArchivedId, "Archived", "مؤرشف"),
        (DocumentStatusType.CanceledId, "Canceled", "ملغي"),
    };

    protected override DocumentStatusType BuildItem(int id, string desc, string descAr) =>
        DocumentStatusType.Create(
            id,
            desc,
            descAr,
            isTerminal: id is DocumentStatusType.ArchivedId or DocumentStatusType.CanceledId).Value;
}

/// <summary>Seeds the document confidentiality levels lookup (Order 70, every environment).</summary>
public sealed class DocumentConfidentialityLevelSeeder : LookupSeederBase<DocumentConfidentialityLevel>
{
    public override int Order => 70;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (DocumentConfidentialityLevel.NormalId, "Normal", "عادي"),
        (DocumentConfidentialityLevel.ConfidentialId, "Confidential", "سري"),
        (DocumentConfidentialityLevel.SecretId, "Secret", "سري للغاية"),
        (DocumentConfidentialityLevel.TopSecretId, "Top secret", "بالغ السرية"),
    };

    protected override DocumentConfidentialityLevel BuildItem(int id, string desc, string descAr) =>
        DocumentConfidentialityLevel.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the document priority levels lookup (Order 75, every environment).</summary>
public sealed class DocumentPriorityLevelSeeder : LookupSeederBase<DocumentPriorityLevel>
{
    public override int Order => 75;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (DocumentPriorityLevel.NormalId, "Normal", "عادي"),
        (DocumentPriorityLevel.UrgentId, "Urgent", "عاجل"),
        (DocumentPriorityLevel.ImmediateId, "Immediate", "فوري"),
    };

    protected override DocumentPriorityLevel BuildItem(int id, string desc, string descAr) =>
        DocumentPriorityLevel.Create(id, desc, descAr).Value;
}
