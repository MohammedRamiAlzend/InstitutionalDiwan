using InstitutionalDiwan.Domain.Entities.Movements;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>Seeds the movement type lookup (Order 47, every environment).</summary>
public sealed class MovementTypeSeeder : LookupSeederBase<MovementType>
{
    public override int Order => 47;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (MovementType.ReferralId, "Referral", "إحالة"),
        (MovementType.TransferId, "Transfer", "تحويل"),
        (MovementType.CcId, "CC", "نسخة"),
        (MovementType.ReturnId, "Return", "إعادة"),
    };

    protected override MovementType BuildItem(int id, string desc, string descAr) =>
        MovementType.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the movement status lookup (Order 48, every environment).</summary>
public sealed class MovementStatusSeeder : LookupSeederBase<MovementStatus>
{
    public override int Order => 48;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (MovementStatus.DraftId, "Draft", "مسودة"),
        (MovementStatus.SentId, "Sent", "مُرسل"),
        (MovementStatus.ReceivedId, "Received", "مستلم"),
        (MovementStatus.InProgressId, "In progress", "قيد المعالجة"),
        (MovementStatus.CompletedId, "Completed", "مكتمل"),
        (MovementStatus.RejectedId, "Rejected", "مرفوض"),
        (MovementStatus.ReturnedId, "Returned", "مُعاد"),
        (MovementStatus.RedirectedId, "Redirected", "مُحوّل"),
        (MovementStatus.ClosedId, "Closed", "مغلق"),
        (MovementStatus.CanceledId, "Canceled", "ملغي"),
    };

    protected override MovementStatus BuildItem(int id, string desc, string descAr) =>
        MovementStatus.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the movement step status lookup (Order 49, every environment).</summary>
public sealed class MovementStepStatusSeeder : LookupSeederBase<MovementStepStatus>
{
    public override int Order => 49;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (MovementStepStatus.PendingId, "Pending", "قيد الانتظار"),
        (MovementStepStatus.SentId, "Sent", "مُرسل"),
        (MovementStepStatus.ReceivedId, "Received", "مستلم"),
        (MovementStepStatus.CompletedId, "Completed", "مكتمل"),
        (MovementStepStatus.RejectedId, "Rejected", "مرفوض"),
        (MovementStepStatus.ReturnedId, "Returned", "مُعاد"),
        (MovementStepStatus.RedirectedId, "Redirected", "مُحوّل"),
    };

    protected override MovementStepStatus BuildItem(int id, string desc, string descAr) =>
        MovementStepStatus.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the movement event type lookup (Order 51, every environment).</summary>
public sealed class MovementEventTypeSeeder : LookupSeederBase<MovementEventType>
{
    public override int Order => 51;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (MovementEventType.CreatedId, "Created", "إنشاء"),
        (MovementEventType.SentId, "Sent", "إرسال"),
        (MovementEventType.ReceivedId, "Received", "استلام"),
        (MovementEventType.CompletedId, "Completed", "إكمال"),
        (MovementEventType.RejectedId, "Rejected", "رفض"),
        (MovementEventType.ReturnedId, "Returned", "إعادة"),
        (MovementEventType.RedirectedId, "Redirected", "تحويل"),
        (MovementEventType.ClosedId, "Closed", "إغلاق"),
        (MovementEventType.CanceledId, "Canceled", "إلغاء"),
    };

    protected override MovementEventType BuildItem(int id, string desc, string descAr) =>
        MovementEventType.Create(id, desc, descAr).Value;
}
