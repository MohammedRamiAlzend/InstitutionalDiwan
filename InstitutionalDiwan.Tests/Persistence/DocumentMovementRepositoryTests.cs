using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>
/// Exercises the movement queue queries against a real SQLite database, so the SQL EF Core
/// generates for the inbox and outbox filters is verified and not only the in-memory fake.
/// </summary>
public sealed class DocumentMovementRepositoryTests
{
    private static readonly Guid DiwanId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid HrId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid FinanceId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ActorId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid DocumentId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static async Task<DiwanDbContext> CreateContextAsync()
    {
        var context = TestContextFactory.Create();

        context.MovementTypes.Add(MovementType.Create(MovementType.ReferralId, "Referral", "إحالة").Value);
        context.MovementStatuses.Add(MovementStatus.Create(MovementStatus.DraftId, "Draft", "مسودة").Value);
        context.MovementStatuses.Add(MovementStatus.Create(MovementStatus.SentId, "Sent", "مرسل").Value);
        context.MovementStatuses.Add(MovementStatus.Create(MovementStatus.ReceivedId, "Received", "مستلم").Value);
        context.MovementStatuses.Add(MovementStatus.Create(MovementStatus.CompletedId, "Completed", "منجز").Value);
        context.MovementStatuses.Add(MovementStatus.Create(MovementStatus.RejectedId, "Rejected", "مرفوض").Value);
        context.MovementStatuses.Add(MovementStatus.Create(MovementStatus.ReturnedId, "Returned", "مُعاد").Value);
        context.MovementStatuses.Add(MovementStatus.Create(MovementStatus.ClosedId, "Closed", "مغلق").Value);
        context.MovementStatuses.Add(MovementStatus.Create(MovementStatus.CanceledId, "Canceled", "ملغى").Value);
        context.MovementStepStatuses.Add(MovementStepStatus.Create(MovementStepStatus.PendingId, "Pending", "قيد الانتظار").Value);
        context.MovementStepStatuses.Add(MovementStepStatus.Create(MovementStepStatus.SentId, "Sent", "مرسل").Value);
        context.MovementStepStatuses.Add(MovementStepStatus.Create(MovementStepStatus.ReceivedId, "Received", "مستلم").Value);
        context.MovementStepStatuses.Add(MovementStepStatus.Create(MovementStepStatus.CompletedId, "Completed", "منجز").Value);
        context.MovementStepStatuses.Add(MovementStepStatus.Create(MovementStepStatus.RejectedId, "Rejected", "مرفوض").Value);
        context.MovementStepStatuses.Add(MovementStepStatus.Create(MovementStepStatus.ReturnedId, "Returned", "مُعاد").Value);
        context.MovementStepStatuses.Add(MovementStepStatus.Create(MovementStepStatus.RedirectedId, "Redirected", "محوَّل").Value);
        context.MovementEventTypes.Add(MovementEventType.Create(MovementEventType.CreatedId, "Created", "تم الإنشاء").Value);
        context.MovementEventTypes.Add(MovementEventType.Create(MovementEventType.SentId, "Sent", "تم الإرسال").Value);
        context.MovementEventTypes.Add(MovementEventType.Create(MovementEventType.ReceivedId, "Received", "تم الاستلام").Value);
        context.MovementEventTypes.Add(MovementEventType.Create(MovementEventType.CompletedId, "Completed", "تم الإنجاز").Value);
        context.MovementEventTypes.Add(MovementEventType.Create(MovementEventType.RejectedId, "Rejected", "تم الرفض").Value);
        context.MovementEventTypes.Add(MovementEventType.Create(MovementEventType.ReturnedId, "Returned", "تم الإعادة").Value);
        context.MovementEventTypes.Add(MovementEventType.Create(MovementEventType.ClosedId, "Closed", "تم الإغلاق").Value);
        context.MovementEventTypes.Add(MovementEventType.Create(MovementEventType.RedirectedId, "Redirected", "تم التحويل").Value);

        await context.SaveChangesAsync();
        return context;
    }

    private static DocumentMovement NewMovement(Guid source, Guid destination) =>
        DocumentMovement.Create(
            DocumentId,
            source,
            destination,
            ActorId,
            MovementType.ReferralId).Value;

    [Fact]
    public async Task ListInboxAsync_ReturnsMovementsAwaitingTheDestinationDepartment()
    {
        await using var context = await CreateContextAsync();
        var repository = new DocumentMovementRepository(context);

        var sent = NewMovement(DiwanId, HrId);
        sent.Send(ActorId);
        context.DocumentMovements.Add(sent);

        var draft = NewMovement(DiwanId, FinanceId);
        context.DocumentMovements.Add(draft);

        await context.SaveChangesAsync();

        var inbox = await repository.ListInboxAsync(HrId);

        Assert.Single(inbox);
        Assert.Equal(sent.MovementId, inbox[0].MovementId);
    }

    [Fact]
    public async Task ListInboxAsync_ExcludesClosedMovements()
    {
        await using var context = await CreateContextAsync();
        var repository = new DocumentMovementRepository(context);

        var closed = NewMovement(DiwanId, HrId);
        closed.Send(ActorId);
        closed.Close(ActorId);
        context.DocumentMovements.Add(closed);

        await context.SaveChangesAsync();

        Assert.Empty(await repository.ListInboxAsync(HrId));
    }

    [Fact]
    public async Task ListOutboxAsync_ReturnsMovementsStillWithTheReceivingSide()
    {
        await using var context = await CreateContextAsync();
        var repository = new DocumentMovementRepository(context);

        var sent = NewMovement(DiwanId, HrId);
        sent.Send(ActorId);
        context.DocumentMovements.Add(sent);

        var draft = NewMovement(DiwanId, FinanceId);
        context.DocumentMovements.Add(draft);

        await context.SaveChangesAsync();

        var outbox = await repository.ListOutboxAsync(DiwanId);

        Assert.Single(outbox);
        Assert.Equal(sent.MovementId, outbox[0].MovementId);
    }

    [Fact]
    public async Task ListOutboxAsync_FollowsTheCurrentStepAfterARedirect()
    {
        await using var context = await CreateContextAsync();
        var repository = new DocumentMovementRepository(context);

        var movement = NewMovement(DiwanId, HrId);
        movement.Send(ActorId);
        movement.RedirectTo(FinanceId, ActorId);
        context.DocumentMovements.Add(movement);

        await context.SaveChangesAsync();

        // Custody moved on: the document now sits in Finance's inbox and in HR's outbox.
        Assert.Empty(await repository.ListOutboxAsync(DiwanId));
        Assert.Single(await repository.ListOutboxAsync(HrId));
        Assert.Empty(await repository.ListOutboxAsync(FinanceId));
        Assert.Single(await repository.ListInboxAsync(FinanceId));
        Assert.Empty(await repository.ListInboxAsync(HrId));
    }

    [Fact]
    public async Task ListInboxAsync_ReturnsRejectedDocumentsToTheSendingDepartment()
    {
        await using var context = await CreateContextAsync();
        var repository = new DocumentMovementRepository(context);

        var movement = NewMovement(DiwanId, HrId);
        movement.Send(ActorId);
        movement.RejectStep(ActorId, "غير مطابق");
        context.DocumentMovements.Add(movement);

        await context.SaveChangesAsync();

        Assert.Empty(await repository.ListInboxAsync(HrId));
        Assert.Single(await repository.ListInboxAsync(DiwanId));
    }
}
