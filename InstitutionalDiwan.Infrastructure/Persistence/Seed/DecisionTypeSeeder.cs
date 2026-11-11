using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Decisions;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the five decision types tied to the base-record document type (Order 320, every mode).
/// Abandonment and indictment stay inactive until an administrator names their departments.
/// Suspicion is activated only when the standard organization's individual diwan exists, and that
/// link is filled in on a later run if the department appears after the type was seeded.
/// </summary>
public sealed class DecisionTypeSeeder : ISeeder
{
    public static readonly Guid AbandonmentId = Guid.Parse("a1000001-0000-4000-8000-000000000001");
    public static readonly Guid SuspicionId = Guid.Parse("a1000002-0000-4000-8000-000000000002");
    public static readonly Guid IndictmentId = Guid.Parse("a1000003-0000-4000-8000-000000000003");
    public static readonly Guid StayOfProceedingsId = Guid.Parse("a1000004-0000-4000-8000-000000000004");
    public static readonly Guid UnificationId = Guid.Parse("a1000005-0000-4000-8000-000000000005");

    public int Order => 320;

    public IReadOnlyCollection<string> Environments { get; } = new[] { SeedingEnvironments.All };

    public IReadOnlyCollection<string> Modes { get; } = SeedingModes.AnyMode;

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var db = context.Db;
        var individual = await db.Departments
            .Where(department => department.Code == StandardDepartmentSeeder.IndividualDiwanCode)
            .Select(department => (Guid?)department.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);

        await EnsureAsync(db, AbandonmentId, "التخلي", isAppealable: false, DecisionEffect.ReferId, isActive: false, destination: null, cancellationToken);
        await EnsureAsync(db, IndictmentId, "الاتهام", isAppealable: true, DecisionEffect.ReferId, isActive: false, destination: null, cancellationToken);
        await EnsureAsync(db, StayOfProceedingsId, "منع المحاكمة", isAppealable: false, DecisionEffect.SendToArchiveId, isActive: true, destination: null, cancellationToken);
        await EnsureAsync(db, UnificationId, "التوحيد", isAppealable: false, DecisionEffect.JoinId, isActive: true, destination: null, cancellationToken);
        await EnsureAsync(
            db, SuspicionId, "الظن", isAppealable: true, DecisionEffect.ReferId,
            isActive: individual is not null, destination: individual, cancellationToken);

        if (individual is { } departmentId)
            await LinkSuspicionAsync(db, departmentId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAsync(
        DiwanDbContext db,
        Guid id,
        string nameAr,
        bool isAppealable,
        int effectId,
        bool isActive,
        Guid? destination,
        CancellationToken cancellationToken)
    {
        if (await db.DecisionTypes.AnyAsync(type => type.DecisionTypeId == id, cancellationToken))
            return;

        var created = DecisionType.Create(
            nameAr, isAppealable, effectId, destination,
            documentTypeIds: [DocumentType.BaseRecordId],
            isActive: isActive,
            decisionTypeId: id);
        if (created.IsFailure)
            throw new InvalidOperationException($"Decision type '{nameAr}' could not be seeded: {created.Error.Code}");

        db.DecisionTypes.Add(created.Value);
    }

    private static async Task LinkSuspicionAsync(
        DiwanDbContext db,
        Guid individualDepartmentId,
        CancellationToken cancellationToken)
    {
        var suspicion = await db.DecisionTypes
            .Include(type => type.DocumentTypes)
            .FirstOrDefaultAsync(type => type.DecisionTypeId == SuspicionId, cancellationToken);
        if (suspicion is null || suspicion.DestinationDepartmentId is not null)
            return;

        var documentTypeIds = suspicion.DocumentTypes.Select(link => link.DocumentTypeId).ToList();
        var updated = suspicion.Update(
            suspicion.NameAr, suspicion.IsAppealable, suspicion.EffectId,
            individualDepartmentId, documentTypeIds);
        if (updated.IsFailure)
            throw new InvalidOperationException($"Suspicion could not be linked: {updated.Error.Code}");

        var activated = suspicion.Activate();
        if (activated.IsFailure)
            throw new InvalidOperationException($"Suspicion could not be activated: {activated.Error.Code}");
    }
}
