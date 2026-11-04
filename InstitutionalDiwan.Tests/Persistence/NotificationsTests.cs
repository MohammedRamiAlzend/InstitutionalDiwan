using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Application.Features.Notifications;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Infrastructure.BackgroundJobs;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>
/// Module 10: the notification aggregate rules, the recipient rules of the publisher, the inbox
/// queries, and the outbox delivery loop, against a real SQLite database. Foreign keys are switched
/// off in the fixture because those constraints are verified by the SQL Server migration instead.
/// </summary>
public sealed class NotificationsTests
{
    private static readonly DateTime Now = TestClock.Instance.UtcNow;

    private static readonly Guid SourceDept = Guid.NewGuid();
    private static readonly Guid TargetDept = Guid.NewGuid();

    private sealed class World
    {
        public DiwanDbContext Db { get; } = TestContextFactory.Create();
        public NotificationRepository Notifications { get; }
        public NotificationRecipientDirectory Directory { get; }
        public FakeDocumentRepository Documents { get; } = new();
        public User Sender { get; }
        public User Clerk { get; }
        public User Manager { get; }

        public World()
        {
            Db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
            Notifications = new NotificationRepository(Db);
            Directory = new NotificationRecipientDirectory(Db);

            Sender = User.Create("sender", "hash", SourceDept).Value;
            Clerk = User.Create("clerk", "hash", TargetDept).Value;
            Manager = User.Create("manager", "hash", TargetDept).Value;
            Db.Users.AddRange(Sender, Clerk, Manager);

            foreach (var id in Enumerable.Range(1, 11))
            {
                Db.NotificationTypes.Add(NotificationType.Create(
                    id, $"Type {id}", $"نوع {id}", "عنوان {Title}", "نص {Title} {Number}").Value);
            }

            Db.SaveChanges();
        }

        public NotificationPublisher Publisher() =>
            new(Notifications, Documents, Directory, Db, new ThrowingLogger<NotificationPublisher>());

        public Document NewDocument(int confidentiality = DocumentConfidentialityLevel.NormalId, Guid? createdBy = null)
        {
            var document = Document.CreateDraft(
                "طلب", DocumentType.InternalMemoId, DocumentDirection.InternalId, SourceDept,
                createdBy ?? Sender.UserId, confidentialityLevelId: confidentiality).Value;
            Documents.Add(document);
            Db.Documents.Add(document);
            Db.SaveChanges();
            return document;
        }

        public DocumentMovement SentMovement(Document document)
        {
            var movement = DocumentMovement.Create(
                document.DocumentId, SourceDept, TargetDept, Sender.UserId, MovementType.ReferralId).Value;
            movement.Send(Sender.UserId);
            return movement;
        }
    }

    private static Notification NewNotification(Guid documentId = default) =>
        Notification.Create(
            NotificationType.MovementSentId, "عنوان", "نص", NotificationPriority.NormalId,
            documentId == default ? null : documentId, null, "/documents/x", Now).Value;

    // ── Aggregate rules ──────────────────────────────────────────────────────

    [Fact]
    public void Create_RejectsTooLongTitleAndBody()
    {
        var title = Notification.Create(1, new string('أ', 301), "نص", 1, null, null, null, Now);
        var body = Notification.Create(1, "عنوان", new string('أ', 2001), 1, null, null, null, Now);

        Assert.Equal(DomainErrors.Notifications.TitleTooLong.Code, title.Error.Code);
        Assert.Equal(DomainErrors.Notifications.BodyTooLong.Code, body.Error.Code);
    }

    [Theory]
    [InlineData("https://evil.example/x")]
    [InlineData("//evil.example/x")]
    [InlineData("documents/1")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/a\\b")]
    public void Create_RejectsActionUrlsThatCouldLeaveTheApp(string actionUrl)
    {
        var result = Notification.Create(1, "عنوان", "نص", 1, null, null, actionUrl, Now);

        Assert.Equal(DomainErrors.Notifications.ActionUrlInvalid.Code, result.Error.Code);
    }

    [Fact]
    public void Create_RejectsActionUrlOverLimit_AndAcceptsRelativePath()
    {
        var tooLong = Notification.Create(1, "عنوان", "نص", 1, null, null, "/" + new string('a', 500), Now);
        var ok = Notification.Create(1, "عنوان", "نص", 1, null, null, "/documents/123?tab=fields", Now);

        Assert.True(tooLong.IsFailure);
        Assert.True(ok.IsSuccess);
    }

    [Fact]
    public void AddRecipient_RejectsMoreThanTheRecipientLimit()
    {
        var notification = NewNotification();
        for (var i = 0; i < Notification.MaxRecipients; i++)
            Assert.True(notification.AddRecipient(Guid.NewGuid(), null, Now).IsSuccess);

        var overflow = notification.AddRecipient(Guid.NewGuid(), null, Now);

        Assert.Equal(DomainErrors.Notifications.TooManyRecipients.Code, overflow.Error.Code);
    }

    [Fact]
    public void EnqueueOutbox_RejectsPayloadOverLimit()
    {
        var result = NewNotification().EnqueueOutbox(
            NotificationChannel.InAppId, new string('x', NotificationOutbox.MaxPayloadLength + 1), Now);

        Assert.Equal(DomainErrors.Notifications.PayloadTooLong.Code, result.Error.Code);
    }

    // ── Publisher recipient rules ────────────────────────────────────────────

    [Fact]
    public async Task PublishMovement_Sent_NotifiesTheDestinationDepartmentExceptTheActor()
    {
        var world = new World();
        var document = world.NewDocument();
        var movement = world.SentMovement(document);

        await world.Publisher().PublishMovementAsync(
            NotificationType.MovementSentId, movement, world.Sender.UserId);
        await world.Db.SaveChangesAsync();

        var notification = await world.Db.Notifications
            .Include(n => n.Recipients).Include(n => n.OutboxEntries).SingleAsync();
        Assert.Equal(
            new[] { world.Clerk.UserId, world.Manager.UserId }.Order(),
            notification.Recipients.Select(r => r.UserId).Order());
        Assert.Equal(TargetDept, notification.Recipients.First().DepartmentId);
        Assert.Equal($"/documents/{document.DocumentId}", notification.ActionUrl);
        Assert.Equal(movement.MovementId, notification.RelatedMovementId);
        var outbox = Assert.Single(notification.OutboxEntries);
        Assert.Equal(NotificationChannel.InAppId, outbox.ChannelId);
        Assert.Equal(NotificationOutboxStatus.PendingId, outbox.StatusId);
        Assert.Contains("طلب", notification.TitleAr);
    }

    [Fact]
    public async Task PublishMovement_Received_NotifiesTheSourceDepartmentAndTheCreator()
    {
        var world = new World();
        var movement = world.SentMovement(world.NewDocument());

        await world.Publisher().PublishMovementAsync(
            NotificationType.MovementReceivedId, movement, world.Clerk.UserId);
        await world.Db.SaveChangesAsync();

        var recipients = await world.Db.NotificationRecipients.Select(r => r.UserId).ToListAsync();
        Assert.Equal(new[] { world.Sender.UserId }, recipients);
    }

    [Fact]
    public async Task PublishMovement_NeverNotifiesWhenTheActorIsTheOnlyCandidate()
    {
        var world = new World();
        var movement = world.SentMovement(world.NewDocument());

        await world.Publisher().PublishMovementAsync(
            NotificationType.MovementClosedId, movement, world.Sender.UserId);
        await world.Db.SaveChangesAsync();

        Assert.Empty(await world.Db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task PublishMovement_SecretDocument_OnlyReachesTheCreatorAndGrantHolders()
    {
        var world = new World();
        var document = world.NewDocument(DocumentConfidentialityLevel.SecretId);
        var movement = world.SentMovement(document);
        world.Db.DocumentAccessGrants.Add(DocumentAccessGrant.Grant(
            document.DocumentId, world.Manager.UserId, AccessLevel.ViewId, world.Sender.UserId, utcNow: Now).Value);
        await world.Db.SaveChangesAsync();

        await world.Publisher().PublishMovementAsync(
            NotificationType.MovementSentId, movement, world.Sender.UserId);
        await world.Db.SaveChangesAsync();

        var recipients = await world.Db.NotificationRecipients.Select(r => r.UserId).ToListAsync();
        Assert.Equal(new[] { world.Manager.UserId }, recipients);
    }

    [Fact]
    public async Task PublishMovement_SkipsInactiveUsers()
    {
        var world = new World();
        world.Clerk.Deactivate();
        await world.Db.SaveChangesAsync();
        var movement = world.SentMovement(world.NewDocument());

        await world.Publisher().PublishMovementAsync(
            NotificationType.MovementSentId, movement, world.Sender.UserId);
        await world.Db.SaveChangesAsync();

        var recipients = await world.Db.NotificationRecipients.Select(r => r.UserId).ToListAsync();
        Assert.Equal(new[] { world.Manager.UserId }, recipients);
    }

    [Fact]
    public async Task PublishDocument_NotifiesTheCreatorUnlessTheyAreTheActor()
    {
        var world = new World();
        var document = world.NewDocument(createdBy: world.Clerk.UserId);

        await world.Publisher().PublishDocumentAsync(
            NotificationType.DocumentArchivedId, document, world.Clerk.UserId);
        await world.Publisher().PublishDocumentAsync(
            NotificationType.DocumentArchivedId, document, world.Manager.UserId);
        await world.Db.SaveChangesAsync();

        var notification = await world.Db.Notifications.Include(n => n.Recipients).SingleAsync();
        Assert.Equal(world.Clerk.UserId, Assert.Single(notification.Recipients).UserId);
    }

    [Fact]
    public async Task Publisher_NeverThrowsWhenTheTypeIsMissing()
    {
        var world = new World();
        var document = world.NewDocument(createdBy: world.Clerk.UserId);

        await world.Publisher().PublishDocumentAsync(99, document, world.Manager.UserId);
        await world.Db.SaveChangesAsync();

        Assert.Empty(await world.Db.Notifications.ToListAsync());
    }

    // ── Inbox queries and commands ───────────────────────────────────────────

    private static async Task<(Notification First, Notification Second)> SeedInboxAsync(World world)
    {
        var first = NewNotification();
        first.AddRecipient(world.Clerk.UserId, TargetDept, Now.AddMinutes(-2));
        first.AddRecipient(world.Manager.UserId, TargetDept, Now.AddMinutes(-2));
        var second = Notification.Create(
            NotificationType.MovementReceivedId, "ثانٍ", "نص", NotificationPriority.NormalId, null, null, null, Now).Value;
        second.AddRecipient(world.Clerk.UserId, TargetDept, Now.AddMinutes(-1));
        world.Db.Notifications.AddRange(first, second);
        await world.Db.SaveChangesAsync();
        return (first, second);
    }

    [Fact]
    public async Task GetInbox_ReturnsOnlyTheCallersRowsNewestFirst_AndFiltersByTypeAndRead()
    {
        var world = new World();
        await SeedInboxAsync(world);

        var all = await world.Notifications.GetInboxAsync(world.Clerk.UserId, false, null, new PageRequest(1, 20));
        Assert.Equal(2, all.TotalCount);
        Assert.Equal("ثانٍ", all.Items[0].TitleAr);

        var byType = await world.Notifications.GetInboxAsync(
            world.Clerk.UserId, false, NotificationType.MovementSentId, new PageRequest(1, 20));
        Assert.Single(byType.Items);

        var other = await world.Notifications.GetInboxAsync(world.Manager.UserId, false, null, new PageRequest(1, 20));
        Assert.Equal(1, other.TotalCount);

        var paged = await world.Notifications.GetInboxAsync(world.Clerk.UserId, false, null, new PageRequest(2, 1));
        Assert.Equal(2, paged.TotalCount);
        Assert.Single(paged.Items);
    }

    [Fact]
    public async Task GetInbox_Search_MatchesTitleOrBody_AndTreatsWildcardsAsLiteralText()
    {
        var world = new World();
        await SeedInboxAsync(world);
        var percent = Notification.Create(
            NotificationType.MovementSentId, "خصم 100% على الرسوم", "تفاصيل", NotificationPriority.NormalId, null, null, null, Now).Value;
        percent.AddRecipient(world.Clerk.UserId, TargetDept, Now);
        world.Db.Notifications.Add(percent);
        await world.Db.SaveChangesAsync();

        var byBody = await world.Notifications.GetInboxAsync(
            world.Clerk.UserId, false, null, new PageRequest(1, 20, "نص"));
        Assert.Equal(2, byBody.TotalCount);

        var byTitle = await world.Notifications.GetInboxAsync(
            world.Clerk.UserId, false, null, new PageRequest(1, 20, "ثانٍ"));
        Assert.Equal("ثانٍ", Assert.Single(byTitle.Items).TitleAr);

        var literalPercent = await world.Notifications.GetInboxAsync(
            world.Clerk.UserId, false, null, new PageRequest(1, 20, "%"));
        Assert.Equal("خصم 100% على الرسوم", Assert.Single(literalPercent.Items).TitleAr);

        var underscore = await world.Notifications.GetInboxAsync(
            world.Clerk.UserId, false, null, new PageRequest(1, 20, "_"));
        Assert.Empty(underscore.Items);

        var otherUser = await world.Notifications.GetInboxAsync(
            world.Manager.UserId, false, null, new PageRequest(1, 20, "ثانٍ"));
        Assert.Empty(otherUser.Items);

        var withFilters = await world.Notifications.GetInboxAsync(
            world.Clerk.UserId, false, NotificationType.MovementSentId, new PageRequest(1, 20, "نص"));
        Assert.Equal(1, withFilters.TotalCount);
    }

    [Fact]
    public async Task GetInbox_SortsByTheRequestedColumn_AndKeepsPagesStable()
    {
        var world = new World();
        await SeedInboxAsync(world);
        var urgent = Notification.Create(
            NotificationType.MovementRejectedId, "عاجل", "نص", NotificationPriority.UrgentId, null, null, null, Now).Value;
        urgent.AddRecipient(world.Clerk.UserId, TargetDept, Now);
        world.Db.Notifications.Add(urgent);
        await world.Db.SaveChangesAsync();
        var userId = world.Clerk.UserId;

        async Task<string[]> Titles(string? sortBy, bool descending, int page = 1, int pageSize = 20) =>
            (await world.Notifications.GetInboxAsync(
                userId, false, null, new PageRequest(page, pageSize, null, sortBy, descending)))
            .Items.Select(item => item.TitleAr).ToArray();

        Assert.Equal(new[] { "عاجل", "ثانٍ", "عنوان" }, await Titles(null, false));
        Assert.Equal(new[] { "عاجل", "ثانٍ", "عنوان" }, await Titles(null, true));
        Assert.Equal(new[] { "عنوان", "ثانٍ", "عاجل" }, await Titles("deliveredAt", false));
        Assert.Equal(new[] { "عاجل", "ثانٍ", "عنوان" }, await Titles("deliveredAt", true));
        Assert.Equal("عاجل", (await Titles("priority", true))[0]);
        Assert.Equal("عاجل", (await Titles("priority", false))[2]);
        Assert.Equal("عنوان", (await Titles("type", false))[0]);
        Assert.Equal("عاجل", (await Titles("type", true))[0]);
        Assert.Equal(3, (await Titles("unknown-key", false)).Length);

        var row = await world.Db.NotificationRecipients.FirstAsync(r => r.UserId == userId);
        row.MarkAsRead(Now);
        await world.Db.SaveChangesAsync();
        Assert.Equal(row.NotificationId,
            (await world.Notifications.GetInboxAsync(userId, false, null, new PageRequest(1, 20, null, "isRead", true)))
            .Items[0].NotificationId);
        Assert.Equal(row.NotificationId,
            (await world.Notifications.GetInboxAsync(userId, false, null, new PageRequest(1, 20, null, "isRead", false)))
            .Items[^1].NotificationId);

        var pagedTitles = new List<string>();
        for (var page = 1; page <= 3; page++)
            pagedTitles.AddRange(await Titles("priority", true, page, 1));
        Assert.Equal(3, pagedTitles.Distinct().Count());
    }

    [Fact]
    public async Task CountUnreadByType_GroupsOnlyTheCallersUnreadRows_AndMatchesTheTotal()
    {
        var world = new World();
        await SeedInboxAsync(world);

        var byType = await world.Notifications.CountUnreadByTypeAsync(world.Clerk.UserId);

        Assert.Equal(2, byType.Count);
        Assert.Equal(1, byType.Single(row => row.TypeId == NotificationType.MovementSentId).Count);
        Assert.Equal(1, byType.Single(row => row.TypeId == NotificationType.MovementReceivedId).Count);
        Assert.Equal(await world.Notifications.CountUnreadAsync(world.Clerk.UserId), byType.Sum(row => row.Count));

        var row = await world.Db.NotificationRecipients.FirstAsync(r => r.UserId == world.Clerk.UserId);
        row.MarkAsRead(Now);
        await world.Db.SaveChangesAsync();

        var afterRead = await world.Notifications.CountUnreadByTypeAsync(world.Clerk.UserId);
        Assert.Equal(1, afterRead.Sum(r => r.Count));
        Assert.DoesNotContain(afterRead, r => r.Count == 0);
        Assert.Empty(await world.Notifications.CountUnreadByTypeAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task MarkReadAndDismiss_AffectOnlyTheOwner_AndAnotherUserGetsNotFound()
    {
        var world = new World();
        await SeedInboxAsync(world);
        var row = await world.Db.NotificationRecipients.FirstAsync(r => r.UserId == world.Clerk.UserId);

        var intruder = await new MarkNotificationReadCommandHandler(world.Notifications, world.Db)
            .Handle(new MarkNotificationReadCommand(row.RecipientId, world.Manager.UserId), CancellationToken.None);
        Assert.Equal(DomainErrors.Notifications.RecipientNotFound.Code, intruder.Error.Code);

        var intruderDismiss = await new DismissNotificationCommandHandler(world.Notifications, world.Db)
            .Handle(new DismissNotificationCommand(row.RecipientId, world.Manager.UserId), CancellationToken.None);
        Assert.Equal(DomainErrors.Notifications.RecipientNotFound.Code, intruderDismiss.Error.Code);

        Assert.Equal(2, await world.Notifications.CountUnreadAsync(world.Clerk.UserId));

        var read = new MarkNotificationReadCommandHandler(world.Notifications, world.Db);
        Assert.True((await read.Handle(new MarkNotificationReadCommand(row.RecipientId, world.Clerk.UserId), CancellationToken.None)).IsSuccess);
        Assert.True((await read.Handle(new MarkNotificationReadCommand(row.RecipientId, world.Clerk.UserId), CancellationToken.None)).IsSuccess);
        Assert.Equal(1, await world.Notifications.CountUnreadAsync(world.Clerk.UserId));

        var dismiss = new DismissNotificationCommandHandler(world.Notifications, world.Db);
        Assert.True((await dismiss.Handle(new DismissNotificationCommand(row.RecipientId, world.Clerk.UserId), CancellationToken.None)).IsSuccess);
        var inbox = await world.Notifications.GetInboxAsync(world.Clerk.UserId, false, null, new PageRequest(1, 20));
        Assert.Single(inbox.Items);
    }

    [Fact]
    public async Task MarkAllRead_ChangesOnlyTheCallersUnreadRows()
    {
        var world = new World();
        await SeedInboxAsync(world);

        var changed = await world.Notifications.MarkAllReadAsync(world.Clerk.UserId, Now);

        Assert.Equal(2, changed);
        Assert.Equal(0, await world.Notifications.CountUnreadAsync(world.Clerk.UserId));
        Assert.Equal(1, await world.Notifications.CountUnreadAsync(world.Manager.UserId));
    }

    [Fact]
    public async Task Purge_RemovesOldReadRowsAndSentOutbox_ButKeepsUnreadAndDeadLetters()
    {
        var world = new World();
        var oldRead = NewNotification();
        oldRead.AddRecipient(world.Clerk.UserId, TargetDept, Now.AddDays(-200));
        var oldUnread = NewNotification();
        oldUnread.AddRecipient(world.Manager.UserId, TargetDept, Now.AddDays(-200));
        world.Db.Notifications.AddRange(oldRead, oldUnread);
        await world.Db.SaveChangesAsync();
        oldRead.Recipients.Single().MarkAsRead(Now.AddDays(-199));

        var sent = NewNotification();
        sent.EnqueueOutbox(NotificationChannel.InAppId, "{}", Now.AddDays(-60));
        var sentOutbox = sent.OutboxEntries.Single();
        sentOutbox.MarkProcessed(Now.AddDays(-59));
        var dead = NewNotification();
        dead.EnqueueOutbox(NotificationChannel.InAppId, "{}", Now.AddDays(-60));
        var deadOutbox = dead.OutboxEntries.Single();
        deadOutbox.RecordFailure("boom", null, maxRetries: 1);
        world.Db.Notifications.AddRange(sent, dead);
        await world.Db.SaveChangesAsync();

        var recipients = await world.Notifications.PurgeRecipientsAsync(Now.AddDays(-90));
        var outbox = await world.Notifications.PurgeSentOutboxAsync(Now.AddDays(-30));

        Assert.Equal(1, recipients);
        Assert.Equal(1, outbox);
        Assert.Equal(NotificationOutboxStatus.DeadLetterId,
            (await world.Db.NotificationOutboxes.SingleAsync()).StatusId);
        Assert.Equal(world.Manager.UserId, (await world.Db.NotificationRecipients.SingleAsync()).UserId);
    }

    // ── Outbox delivery ──────────────────────────────────────────────────────

    private sealed class RecordingNotifier : IRealtimeNotifier
    {
        public bool Fail { get; set; }
        public List<(Guid UserId, NotificationPush Push)> Sent { get; } = new();

        public Task NotifyUserAsync(Guid userId, NotificationPush push, CancellationToken cancellationToken = default)
        {
            if (Fail)
                throw new InvalidOperationException("hub down");
            Sent.Add((userId, push));
            return Task.CompletedTask;
        }
    }

    private static (OutboxProcessorJob Job, IServiceProvider Services) CreateJob(World world, RecordingNotifier notifier)
    {
        var services = new ServiceCollection();
        services.AddSingleton<INotificationRepository>(world.Notifications);
        services.AddSingleton<IUnitOfWork>(world.Db);
        services.AddSingleton<IRealtimeNotifier>(notifier);
        var provider = services.BuildServiceProvider();

        var job = new OutboxProcessorJob(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new NotificationJobsOptions { MaxRetries = 2, RetryBaseSeconds = 10 }),
            NullLogger<OutboxProcessorJob>.Instance);
        return (job, provider);
    }

    [Fact]
    public async Task OutboxJob_DeliversToEveryRecipientAndMarksTheRowSent()
    {
        var world = new World();
        var movement = world.SentMovement(world.NewDocument());
        await world.Publisher().PublishMovementAsync(NotificationType.MovementSentId, movement, world.Sender.UserId);
        await world.Db.SaveChangesAsync();
        var notifier = new RecordingNotifier();
        var (job, provider) = CreateJob(world, notifier);

        await job.ProcessBatchAsync(provider, new NotificationJobsOptions { MaxRetries = 2 }, CancellationToken.None);

        Assert.Equal(2, notifier.Sent.Count);
        Assert.All(notifier.Sent, sent => Assert.Equal("عنوان طلب", sent.Push.TitleAr));
        Assert.Equal(NotificationOutboxStatus.SentId, (await world.Db.NotificationOutboxes.SingleAsync()).StatusId);
    }

    [Fact]
    public async Task OutboxJob_BacksOffOnFailureAndEndsAsDeadLetter()
    {
        var world = new World();
        var movement = world.SentMovement(world.NewDocument());
        await world.Publisher().PublishMovementAsync(NotificationType.MovementSentId, movement, world.Sender.UserId);
        await world.Db.SaveChangesAsync();
        var notifier = new RecordingNotifier { Fail = true };
        var (job, provider) = CreateJob(world, notifier);
        var options = new NotificationJobsOptions { MaxRetries = 2, RetryBaseSeconds = 10 };

        await job.ProcessBatchAsync(provider, options, CancellationToken.None);
        var failed = await world.Db.NotificationOutboxes.SingleAsync();
        Assert.Equal(NotificationOutboxStatus.FailedId, failed.StatusId);
        Assert.Equal(1, failed.RetryCount);
        Assert.NotNull(failed.NextRetryAt);

        // Not due yet: a second poll leaves it alone.
        await job.ProcessBatchAsync(provider, options, CancellationToken.None);
        Assert.Equal(1, (await world.Db.NotificationOutboxes.SingleAsync()).RetryCount);

        // Make it due and fail again: the second failure reaches MaxRetries.
        world.Db.Entry(failed).Property(o => o.NextRetryAt).CurrentValue = Now.AddYears(-1);
        await world.Db.SaveChangesAsync();
        await job.ProcessBatchAsync(provider, options, CancellationToken.None);

        var dead = await world.Db.NotificationOutboxes.SingleAsync();
        Assert.Equal(NotificationOutboxStatus.DeadLetterId, dead.StatusId);
        Assert.Equal(2, dead.RetryCount);
    }
}
