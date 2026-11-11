using InstitutionalDiwan.Application.Features.Decisions;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Decisions;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Entities.Relations;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Events;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class DecisionTests
{
    private static readonly DateOnly DecisionDate = new(2026, 11, 11);

    private sealed class World
    {
        public FakeDocumentRepository Documents { get; } = new();
        public FakeDecisionTypeRepository Types { get; } = new();
        public FakeDocumentDecisionRepository Decisions { get; } = new();
        public FakeDocumentTypeRepository DocumentTypes { get; } = new();
        public FakeDepartmentRepository Departments { get; } = new();
        public FakeDepartmentScopeEvaluator Scope { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();
        public FakeNotificationPublisher Notifications { get; } = new();
        public FakeDocumentRelationRepository Relations { get; } = new();
        public FakeDocumentAccessEvaluator Access { get; } = new() { AllowAll = true };
        public FakeUserContext Users { get; } = new();
        public Guid HolderId { get; } = Guid.NewGuid();
        public Guid UserId { get; } = Guid.NewGuid();
        public Document Document { get; }

        public World()
        {
            Document = Document.CreateDraft(
                "كتاب", DocumentType.InternalMemoId, DocumentDirection.InternalId, HolderId, UserId).Value;
            Documents.Add(Document);
            DocumentTypes.Add(DocumentType.Create(DocumentType.InternalMemoId, "Memo", "مذكرة").Value);
            Scope.AccessibleDepartments.Add(HolderId);
            Decisions.OpenVisit = new OpenDecisionVisit(Guid.NewGuid(), HolderId);
            Relations.Types.Add(DocumentRelationType.Create(
                DocumentRelationType.JoinedIntoId, "Joined into", "موحَّد مع").Value);
            Users.UserId = UserId;
            Users.ScopeTypeId = UserScopeType.SystemId;
        }

        public IssueDocumentDecisionCommandHandler IssueHandler() =>
            new(Documents, Types, Decisions, new FakeDocumentFileRepository(), Scope, UnitOfWork, Notifications, Relations, Access);

        public Task<InstitutionalDiwan.Domain.Common.Result<DocumentDecisionResponse>> IssueAsync(
            DecisionType type, string number, DateOnly? date = null, Guid? documentId = null, Guid? joinTargetDocumentId = null) =>
            IssueHandler().Handle(
                new IssueDocumentDecisionCommand(
                    documentId ?? Document.DocumentId,
                    UserId,
                    type.DecisionTypeId,
                    number,
                    date ?? DecisionDate,
                    null,
                    null,
                    joinTargetDocumentId),
                CancellationToken.None);

        public Document AddDocument(string title)
        {
            var document = Document.CreateDraft(
                title, DocumentType.InternalMemoId, DocumentDirection.InternalId, HolderId, UserId).Value;
            Documents.Add(document);
            return document;
        }

        public CancelDecisionCommandHandler CancelHandler() =>
            new(Decisions, Types, Users, UnitOfWork, Relations);

        public ListDocumentDecisionsQueryHandler ListHandler() =>
            new(Documents, Decisions, Types, Access, Relations);

        public RecordDecisionAppealCommandHandler AppealHandler() =>
            new(Documents, Decisions, Types, Departments, Scope, UnitOfWork);

        public RecordAppealOutcomeCommandHandler OutcomeHandler() =>
            new(Decisions, Types, Scope, UnitOfWork);

        public Task<InstitutionalDiwan.Domain.Common.Result<DocumentDecisionResponse>> AppealAsync(
            Guid decisionId, Guid? appellantDepartmentId = null, string? reference = null) =>
            AppealHandler().Handle(
                new RecordDecisionAppealCommand(
                    decisionId, UserId, DecisionDate, appellantDepartmentId, reference, null),
                CancellationToken.None);

        public Task<InstitutionalDiwan.Domain.Common.Result<DocumentDecisionResponse>> OutcomeAsync(
            Guid decisionId, Guid appealId, int outcomeId) =>
            OutcomeHandler().Handle(
                new RecordAppealOutcomeCommand(
                    decisionId, appealId, UserId, outcomeId, DecisionDate.AddDays(1)),
                CancellationToken.None);
    }

    private static DecisionType Type(
        int effect,
        bool appealable,
        bool active = true,
        int documentTypeId = DocumentType.InternalMemoId,
        Guid? destination = null)
    {
        destination ??= effect == DecisionEffect.ReferId && active ? Guid.NewGuid() : null;
        return DecisionType.Create(
            "نوع", appealable, effect, destination, [documentTypeId], isActive: active).Value;
    }

    [Fact]
    public void Activate_ReferralWithoutDestination_IsRefused()
    {
        var type = Type(DecisionEffect.ReferId, appealable: true, active: false, destination: null);

        var result = type.Activate();

        Assert.Equal(DomainErrors.Decisions.TypeDestinationRequired.Code, result.Error.Code);
        Assert.False(type.IsActive);
    }

    [Fact]
    public async Task Create_ReferralWithoutDestination_IsStoredInactive()
    {
        var world = new World();
        var handler = new CreateDecisionTypeCommandHandler(
            world.Types, world.DocumentTypes, world.Departments, world.UnitOfWork);

        var result = await handler.Handle(new CreateDecisionTypeCommand(
            "التخلي", false, DecisionEffect.ReferId, null, [DocumentType.InternalMemoId]),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.False(result.Value.IsActive);
        Assert.Null(result.Value.DestinationDepartmentId);
    }

    [Fact]
    public async Task Deactivate_LeavesAUsedTypeAndItsDecisionSnapshot()
    {
        var world = new World();
        var destination = Guid.NewGuid();
        var type = Type(DecisionEffect.ReferId, appealable: true, destination: destination);
        world.Types.Add(type);

        var issued = await world.IssueAsync(type, "2026/1");
        Assert.True(issued.IsSuccess, issued.Error?.Code);

        var renamed = type.Update("اسم جديد", false, DecisionEffect.ReferId, Guid.NewGuid(), [DocumentType.InternalMemoId]);
        Assert.True(renamed.IsSuccess, renamed.Error?.Code);
        type.Deactivate();

        var decision = world.Decisions.Decisions.Single();
        Assert.False(type.IsActive);
        Assert.Equal(type.DecisionTypeId, decision.DecisionTypeId);
        Assert.True(decision.IsAppealable);
        Assert.Equal(DecisionEffect.ReferId, decision.EffectId);
        Assert.Equal(destination, decision.DestinationDepartmentId);
        Assert.Equal(DecisionStatus.IssuedId, decision.StatusId);
    }

    [Fact]
    public async Task Issue_NonAppealableType_IsBornFinal()
    {
        var world = new World();
        var type = Type(DecisionEffect.SendToArchiveId, appealable: false);
        world.Types.Add(type);

        var issued = await world.IssueAsync(type, "2026/4");

        Assert.True(issued.IsSuccess, issued.Error?.Code);
        Assert.Equal(DecisionStatus.FinalId, issued.Value.StatusId);
        Assert.False(issued.Value.IsAppealable);
        Assert.Empty(issued.Value.Appeals);
        Assert.Contains(world.Notifications.Published, item => item.TypeId == NotificationType.DecisionIssuedId);
    }

    [Fact]
    public async Task Issue_SecondInForceDecision_IsRefused()
    {
        var world = new World();
        var type = Type(DecisionEffect.NoneId, appealable: false);
        world.Types.Add(type);
        Assert.True((await world.IssueAsync(type, "2026/1")).IsSuccess);

        var second = await world.IssueAsync(type, "2026/2");

        Assert.Equal(DomainErrors.Decisions.ActiveDecisionExists.Code, second.Error.Code);
    }

    [Fact]
    public async Task Issue_FromADepartmentThatDoesNotHoldTheDocument_IsRefused()
    {
        var world = new World();
        var type = Type(DecisionEffect.NoneId, appealable: false);
        world.Types.Add(type);
        world.Decisions.OpenVisit = new OpenDecisionVisit(Guid.NewGuid(), Guid.NewGuid());

        var issued = await world.IssueAsync(type, "2026/1");

        Assert.Equal(DomainErrors.Decisions.NotHoldingDepartment.Code, issued.Error.Code);
    }

    [Fact]
    public async Task Issue_TypeNotLinkedToTheDocumentType_IsRefused()
    {
        var world = new World();
        var type = Type(DecisionEffect.NoneId, appealable: false, documentTypeId: DocumentType.CircularId);
        world.Types.Add(type);

        var issued = await world.IssueAsync(type, "2026/1");

        Assert.Equal(DomainErrors.Decisions.TypeNotAllowedForDocument.Code, issued.Error.Code);
    }

    [Fact]
    public async Task Issue_JoinWithoutAPrincipalFile_IsRefused()
    {
        var world = new World();
        var type = Type(DecisionEffect.JoinId, appealable: false);
        world.Types.Add(type);

        var issued = await world.IssueAsync(type, "2026/1");

        Assert.Equal(DomainErrors.Decisions.JoinTargetInvalid.Code, issued.Error.Code);
        Assert.Empty(world.Decisions.Decisions);
        Assert.Empty(world.Relations.Relations);
    }

    [Fact]
    public async Task Issue_DuplicateNumber_IsRefusedForTheSameDepartmentAndYearOnly()
    {
        var world = new World();
        var type = Type(DecisionEffect.NoneId, appealable: false);
        world.Types.Add(type);

        var first = await world.IssueAsync(type, "2026/1");
        Assert.True(first.IsSuccess, first.Error?.Code);
        Assert.True(world.Decisions.Decisions.Single().Cancel(world.UserId, "أُدخل على الوثيقة الخطأ").IsSuccess);

        var sameYear = await world.IssueAsync(type, "2026/1");
        Assert.Equal(DomainErrors.Decisions.DecisionNumberTaken.Code, sameYear.Error.Code);

        var nextYear = await world.IssueAsync(type, "2026/1", new DateOnly(2027, 1, 2));
        Assert.True(nextYear.IsSuccess, nextYear.Error?.Code);
        Assert.True(world.Decisions.Decisions.Single(decision => decision.DecisionYear == 2027)
            .Cancel(world.UserId, "أُغلق").IsSuccess);

        var otherDepartment = Guid.NewGuid();
        world.Scope.AccessibleDepartments.Add(otherDepartment);
        world.Decisions.OpenVisit = new OpenDecisionVisit(Guid.NewGuid(), otherDepartment);
        var other = await world.IssueAsync(type, "2026/1");
        Assert.True(other.IsSuccess, other.Error?.Code);
    }

    [Fact]
    public async Task Appeal_MovesIssuedToAppealed_AndRejectionMakesItFinal()
    {
        var world = new World();
        var type = Type(DecisionEffect.ReferId, appealable: true);
        world.Types.Add(type);
        var issued = await world.IssueAsync(type, "2026/8");
        Assert.Equal(DecisionStatus.IssuedId, issued.Value.StatusId);
        var notices = world.Notifications.Published.Count;

        var pendingRuling = await world.OutcomeAsync(issued.Value.DecisionId, Guid.NewGuid(), AppealOutcome.PendingId);
        Assert.Equal(DomainErrors.Decisions.AppealNotFound.Code, pendingRuling.Error.Code);

        var appealed = await world.AppealAsync(issued.Value.DecisionId, reference: "ط/1");
        Assert.True(appealed.IsSuccess, appealed.Error?.Code);
        Assert.Equal(DecisionStatus.AppealedId, appealed.Value.StatusId);
        var appeal = Assert.Single(appealed.Value.Appeals);
        Assert.Equal(AppealOutcome.PendingId, appeal.OutcomeId);
        Assert.Equal("ط/1", appeal.ReferenceNumber);
        Assert.Equal(world.HolderId, appeal.AppellantDepartmentId);
        Assert.Equal(notices, world.Notifications.Published.Count);
        Assert.Contains(
            world.Decisions.Decisions.Single().DomainEvents.OfType<DecisionAppealedEvent>(),
            item => item.AppealId == appeal.AppealId);

        var second = await world.AppealAsync(issued.Value.DecisionId);
        Assert.Equal(DomainErrors.Decisions.AppealAlreadyPending.Code, second.Error.Code);

        var notARuling = await world.OutcomeAsync(issued.Value.DecisionId, appeal.AppealId, AppealOutcome.PendingId);
        Assert.Equal(DomainErrors.Decisions.AppealOutcomeInvalid.Code, notARuling.Error.Code);

        var rejected = await world.OutcomeAsync(issued.Value.DecisionId, appeal.AppealId, AppealOutcome.RejectedId);
        Assert.True(rejected.IsSuccess, rejected.Error?.Code);
        Assert.Equal(DecisionStatus.FinalId, rejected.Value.StatusId);
        Assert.Equal(AppealOutcome.RejectedId, Assert.Single(rejected.Value.Appeals).OutcomeId);
        Assert.True(world.Decisions.Decisions.Single().IsInForce);

        var repeated = await world.OutcomeAsync(issued.Value.DecisionId, appeal.AppealId, AppealOutcome.AcceptedId);
        Assert.Equal(DomainErrors.Decisions.AppealOutcomeInvalid.Code, repeated.Error.Code);

        var another = await world.IssueAsync(type, "2026/9");
        Assert.Equal(DomainErrors.Decisions.ActiveDecisionExists.Code, another.Error.Code);
    }

    [Fact]
    public async Task Appeal_OnANonAppealableDecision_IsRefused()
    {
        var world = new World();
        var type = Type(DecisionEffect.SendToArchiveId, appealable: false);
        world.Types.Add(type);
        var issued = await world.IssueAsync(type, "2026/4");
        Assert.Equal(DecisionStatus.FinalId, issued.Value.StatusId);

        var appealed = await world.AppealAsync(issued.Value.DecisionId);
        Assert.Equal(DomainErrors.Decisions.NotAppealable.Code, appealed.Error.Code);
        Assert.Empty(world.Decisions.Decisions.Single().Appeals);
        Assert.Equal(DecisionStatus.FinalId, world.Decisions.Decisions.Single().StatusId);
    }

    [Fact]
    public async Task AcceptedAppeal_RevokesTheDecision_AndAllowsANewOne()
    {
        var world = new World();
        var type = Type(DecisionEffect.NoneId, appealable: true);
        world.Types.Add(type);
        var issued = await world.IssueAsync(type, "2026/1");
        var appealed = await world.AppealAsync(issued.Value.DecisionId);
        Assert.True(appealed.IsSuccess, appealed.Error?.Code);

        var accepted = await world.OutcomeAsync(
            issued.Value.DecisionId, appealed.Value.Appeals.Single().AppealId, AppealOutcome.AcceptedId);
        Assert.True(accepted.IsSuccess, accepted.Error?.Code);
        Assert.Equal(DecisionStatus.RevokedId, accepted.Value.StatusId);
        Assert.Equal(AppealOutcome.AcceptedId, accepted.Value.Appeals.Single().OutcomeId);
        Assert.False(world.Decisions.Decisions.Single(decision => decision.DecisionId == issued.Value.DecisionId).IsInForce);

        var next = await world.IssueAsync(type, "2026/2");
        Assert.True(next.IsSuccess, next.Error?.Code);
        Assert.Equal(DecisionStatus.IssuedId, next.Value.StatusId);
    }

    [Fact]
    public async Task Appeal_OnAnArchivedDocument_IsAllowed_AndLeavesTheDocumentArchived()
    {
        var world = new World();
        var type = Type(DecisionEffect.ReferId, appealable: true);
        world.Types.Add(type);
        var issued = await world.IssueAsync(type, "2026/3");
        Assert.True(issued.IsSuccess, issued.Error?.Code);

        Assert.True(world.Document.Register(
            Guid.NewGuid(), OfficialNumberValue.Create("2026/000099").Value, world.UserId).IsSuccess);
        Assert.True(world.Document.Archive(
            world.UserId, hasActiveMovements: false, ArchiveLocation.PhysicalArchiveId).IsSuccess);
        var documentEvents = world.Document.DomainEvents.Count;

        var appealed = await world.AppealAsync(issued.Value.DecisionId);
        Assert.True(appealed.IsSuccess, appealed.Error?.Code);
        Assert.Equal(DecisionStatus.AppealedId, appealed.Value.StatusId);
        Assert.Equal(DocumentStatusType.ArchivedId, world.Document.CurrentStatusId);
        Assert.Equal(documentEvents, world.Document.DomainEvents.Count);

        var accepted = await world.OutcomeAsync(
            issued.Value.DecisionId, appealed.Value.Appeals.Single().AppealId, AppealOutcome.AcceptedId);
        Assert.True(accepted.IsSuccess, accepted.Error?.Code);
        Assert.Equal(DecisionStatus.RevokedId, accepted.Value.StatusId);
        Assert.Equal(DocumentStatusType.ArchivedId, world.Document.CurrentStatusId);
    }

    [Fact]
    public async Task Appeal_IsAllowedWhenScopeCoversTheIssuerOrTheAppellant()
    {
        var appellant = Department.Create("APP", "النيابة", DepartmentType.SectionId).Value;

        var byAppellant = new World();
        var type = Type(DecisionEffect.NoneId, appealable: true);
        byAppellant.Types.Add(type);
        byAppellant.Departments.Add(appellant);
        var issued = await byAppellant.IssueAsync(type, "2026/5");
        byAppellant.Scope.AccessibleDepartments.Clear();
        byAppellant.Scope.AccessibleDepartments.Add(appellant.DepartmentId);
        var appealed = await byAppellant.AppealAsync(issued.Value.DecisionId, appellant.DepartmentId);
        Assert.True(appealed.IsSuccess, appealed.Error?.Code);
        var ruled = await byAppellant.OutcomeAsync(
            issued.Value.DecisionId, appealed.Value.Appeals.Single().AppealId, AppealOutcome.RejectedId);
        Assert.True(ruled.IsSuccess, ruled.Error?.Code);

        var byIssuer = new World();
        byIssuer.Types.Add(type);
        byIssuer.Departments.Add(appellant);
        var issuerDecision = await byIssuer.IssueAsync(type, "2026/6");
        byIssuer.Scope.AccessibleDepartments.Clear();
        byIssuer.Scope.AccessibleDepartments.Add(byIssuer.HolderId);
        var fromIssuer = await byIssuer.AppealAsync(issuerDecision.Value.DecisionId, appellant.DepartmentId);
        Assert.True(fromIssuer.IsSuccess, fromIssuer.Error?.Code);
        byIssuer.Scope.AccessibleDepartments.Clear();
        var deniedOutcome = await byIssuer.OutcomeAsync(
            issuerDecision.Value.DecisionId, fromIssuer.Value.Appeals.Single().AppealId, AppealOutcome.AcceptedId);
        Assert.Equal(DomainErrors.Decisions.AppealOutOfScope.Code, deniedOutcome.Error.Code);
        Assert.Equal(DecisionStatus.AppealedId, byIssuer.Decisions.Decisions.Single().StatusId);

        var outsider = new World();
        outsider.Types.Add(type);
        var blocked = await outsider.IssueAsync(type, "2026/7");
        outsider.Scope.AccessibleDepartments.Clear();
        var refused = await outsider.AppealAsync(blocked.Value.DecisionId);
        Assert.Equal(DomainErrors.Decisions.AppealOutOfScope.Code, refused.Error.Code);
        Assert.Equal(DecisionStatus.IssuedId, outsider.Decisions.Decisions.Single().StatusId);

        var missingDepartment = Guid.NewGuid();
        outsider.Scope.AccessibleDepartments.Add(outsider.HolderId);
        var unknown = await outsider.AppealAsync(blocked.Value.DecisionId, missingDepartment);
        Assert.Equal(DomainErrors.Department.NotFound.Code, unknown.Error.Code);
    }

    [Fact]
    public async Task Issue_Join_CreatesTheRelationOnTheJoinedFile_AndLeavesItsNumberAndPlace()
    {
        var world = new World();
        var join = Type(DecisionEffect.JoinId, appealable: false);
        var separating = Type(DecisionEffect.NoneId, appealable: false);
        world.Types.Add(join);
        world.Types.Add(separating);
        var principal = world.AddDocument("الرئيسي");
        var status = world.Document.CurrentStatusId;
        var events = world.Document.DomainEvents.Count;

        var issued = await world.IssueAsync(join, "JOIN/1", joinTargetDocumentId: principal.DocumentId);

        Assert.True(issued.IsSuccess, issued.Error?.Code);
        Assert.Equal(DecisionStatus.FinalId, issued.Value.StatusId);
        Assert.Equal(world.Document.DocumentId, issued.Value.DocumentId);
        Assert.Equal(status, world.Document.CurrentStatusId);
        Assert.Equal(events, world.Document.DomainEvents.Count);
        var relation = Assert.Single(world.Relations.Relations);
        Assert.Equal(DocumentRelationType.JoinedIntoId, relation.RelationTypeId);
        Assert.Equal(world.Document.DocumentId, relation.SourceDocumentId);
        Assert.Equal(principal.DocumentId, relation.TargetDocumentId);
        Assert.Equal(RelationActionType.CreatedId, Assert.Single(world.Relations.Events).ActionTypeId);

        var own = await world.IssueAsync(separating, "A/1", documentId: principal.DocumentId);
        Assert.True(own.IsSuccess, own.Error?.Code);
        Assert.Equal(principal.DocumentId, own.Value.DocumentId);
        Assert.Equal(own.Value.DecisionId, (await world.Decisions.GetActiveByDocumentAsync(principal.DocumentId))!.DecisionId);
        Assert.Equal(issued.Value.DecisionId, (await world.Decisions.GetActiveByDocumentAsync(world.Document.DocumentId))!.DecisionId);
    }

    [Fact]
    public async Task Issue_JoinThatClosesACycle_OrTargetsAJoinedFile_IsRefused()
    {
        var world = new World();
        var join = Type(DecisionEffect.JoinId, appealable: false);
        world.Types.Add(join);
        var principal = world.AddDocument("الرئيسي");
        var third = world.AddDocument("ثالث");
        Assert.True((await world.IssueAsync(join, "JOIN/1", joinTargetDocumentId: principal.DocumentId)).IsSuccess);

        var back = await world.IssueAsync(join, "JOIN/2", documentId: principal.DocumentId, joinTargetDocumentId: world.Document.DocumentId);
        Assert.Equal(DomainErrors.Decisions.JoinTargetInvalid.Code, back.Error.Code);

        world.Decisions.Decisions.Clear();
        Assert.True((await world.IssueAsync(join, "JOIN/3", documentId: principal.DocumentId, joinTargetDocumentId: third.DocumentId)).IsSuccess);
        var intoJoined = await world.IssueAsync(join, "JOIN/4", documentId: third.DocumentId, joinTargetDocumentId: principal.DocumentId);
        Assert.Equal(DomainErrors.Decisions.JoinTargetInvalid.Code, intoJoined.Error.Code);
        Assert.Equal(2, world.Relations.Relations.Count);
    }

    [Fact]
    public async Task Issue_JoinTargetTheIssuerCannotView_IsRefused()
    {
        var world = new World();
        var join = Type(DecisionEffect.JoinId, appealable: false);
        world.Types.Add(join);
        var principal = world.AddDocument("محجوب");
        world.Access.AllowAll = false;
        world.Access.ViewableDocuments.Add(world.Document.DocumentId);

        var issued = await world.IssueAsync(join, "JOIN/1", joinTargetDocumentId: principal.DocumentId);

        Assert.Equal(DomainErrors.Decisions.JoinTargetInvalid.Code, issued.Error.Code);
        Assert.Empty(world.Decisions.Decisions);
        Assert.Empty(world.Relations.Relations);
    }

    [Fact]
    public async Task Issue_JoinTargetThatIsMissingArchivedCanceledOrThisFile_IsRefused()
    {
        var world = new World();
        var join = Type(DecisionEffect.JoinId, appealable: false);
        var other = Type(DecisionEffect.NoneId, appealable: false);
        world.Types.Add(join);
        world.Types.Add(other);

        var missing = await world.IssueAsync(join, "JOIN/1", joinTargetDocumentId: Guid.NewGuid());
        Assert.Equal(DomainErrors.Decisions.JoinTargetInvalid.Code, missing.Error.Code);

        var self = await world.IssueAsync(join, "JOIN/1", joinTargetDocumentId: world.Document.DocumentId);
        Assert.Equal(DomainErrors.Decisions.JoinTargetInvalid.Code, self.Error.Code);

        var archived = world.AddDocument("مؤرشف");
        Assert.True(archived.Register(
            Guid.NewGuid(), OfficialNumberValue.Create("2026/000099").Value, world.UserId).IsSuccess);
        Assert.True(archived.Archive(world.UserId, false, ArchiveLocation.PhysicalArchiveId).IsSuccess);
        var archivedJoin = await world.IssueAsync(join, "JOIN/1", joinTargetDocumentId: archived.DocumentId);
        Assert.Equal(DomainErrors.Decisions.JoinTargetInvalid.Code, archivedJoin.Error.Code);

        var canceled = world.AddDocument("ملغى");
        Assert.True(canceled.Cancel(world.UserId, false, "أُلغي").IsSuccess);
        var canceledJoin = await world.IssueAsync(join, "JOIN/1", joinTargetDocumentId: canceled.DocumentId);
        Assert.Equal(DomainErrors.Decisions.JoinTargetInvalid.Code, canceledJoin.Error.Code);

        var withTarget = await world.IssueAsync(other, "N/1", joinTargetDocumentId: world.AddDocument("آخر").DocumentId);
        Assert.Equal(DomainErrors.Decisions.JoinTargetInvalid.Code, withTarget.Error.Code);
        Assert.Empty(world.Relations.Relations);
        Assert.Empty(world.Decisions.Decisions);
    }

    [Fact]
    public async Task Issue_SeparatingDecisionOnAJoinedFile_IsRefused()
    {
        var world = new World();
        var join = Type(DecisionEffect.JoinId, appealable: false);
        var separating = Type(DecisionEffect.SendToArchiveId, appealable: false);
        world.Types.Add(join);
        world.Types.Add(separating);
        var principal = world.AddDocument("الرئيسي");
        Assert.True((await world.IssueAsync(join, "JOIN/1", joinTargetDocumentId: principal.DocumentId)).IsSuccess);

        var second = await world.IssueAsync(separating, "SEP/1");

        Assert.Equal(DomainErrors.Decisions.DocumentJoined.Code, second.Error.Code);
        Assert.Single(world.Decisions.Decisions);
        Assert.Single(world.Relations.Relations);
    }

    [Fact]
    public async Task Cancel_Join_RemovesTheRelationInTheSameSave()
    {
        var world = new World();
        var join = Type(DecisionEffect.JoinId, appealable: false);
        var separating = Type(DecisionEffect.NoneId, appealable: false);
        world.Types.Add(join);
        world.Types.Add(separating);
        var principal = world.AddDocument("الرئيسي");
        var issued = await world.IssueAsync(join, "JOIN/1", joinTargetDocumentId: principal.DocumentId);
        Assert.True(issued.IsSuccess, issued.Error?.Code);

        var canceled = await world.CancelHandler().Handle(
            new CancelDecisionCommand(issued.Value.DecisionId, world.UserId, "أُدخل على الملف الخطأ"),
            CancellationToken.None);

        Assert.True(canceled.IsSuccess, canceled.Error?.Code);
        Assert.Equal(DecisionStatus.CanceledId, canceled.Value.StatusId);
        Assert.Empty(world.Relations.Relations);
        Assert.Contains(world.Relations.Events, relationEvent => relationEvent.ActionTypeId == RelationActionType.RemovedId);
        Assert.Equal(2, world.UnitOfWork.SaveChangesCallCount);

        var again = await world.IssueAsync(separating, "SEP/1");
        Assert.True(again.IsSuccess, again.Error?.Code);
    }

    [Fact]
    public async Task List_OnTheJoinedFile_HidesThePrincipalDecisionsWhenTheCallerCannotViewIt()
    {
        var world = new World();
        var join = Type(DecisionEffect.JoinId, appealable: false);
        var separating = Type(DecisionEffect.NoneId, appealable: false);
        world.Types.Add(join);
        world.Types.Add(separating);
        var principal = world.AddDocument("الرئيسي");
        var own = await world.IssueAsync(separating, "A/1", documentId: principal.DocumentId);
        var issued = await world.IssueAsync(join, "JOIN/1", joinTargetDocumentId: principal.DocumentId);
        Assert.True(own.IsSuccess, own.Error?.Code);
        Assert.True(issued.IsSuccess, issued.Error?.Code);

        var visible = await world.ListHandler().Handle(
            new ListDocumentDecisionsQuery(world.Document.DocumentId), CancellationToken.None);
        Assert.True(visible.IsSuccess, visible.Error?.Code);
        Assert.Contains(visible.Value, decision => decision.DecisionId == own.Value.DecisionId);
        Assert.Contains(visible.Value, decision => decision.DecisionId == issued.Value.DecisionId);

        var principalOnly = await world.ListHandler().Handle(
            new ListDocumentDecisionsQuery(principal.DocumentId), CancellationToken.None);
        Assert.DoesNotContain(principalOnly.Value, decision => decision.DecisionId == issued.Value.DecisionId);

        world.Access.AllowAll = false;
        world.Access.ViewableDocuments.Add(world.Document.DocumentId);
        var hidden = await world.ListHandler().Handle(
            new ListDocumentDecisionsQuery(world.Document.DocumentId), CancellationToken.None);
        Assert.True(hidden.IsSuccess, hidden.Error?.Code);
        var only = Assert.Single(hidden.Value);
        Assert.Equal(issued.Value.DecisionId, only.DecisionId);
    }

    [Fact]
    public async Task NextAction_SuggestsAnOrdinaryMovementWhenThePrincipalFileIsHeldElsewhere()
    {
        var world = new World();
        var join = Type(DecisionEffect.JoinId, appealable: false);
        world.Types.Add(join);
        var principal = world.AddDocument("الرئيسي");
        var otherDepartment = Guid.NewGuid();
        world.Decisions.VisitsByDocument[principal.DocumentId] =
            new OpenDecisionVisit(Guid.NewGuid(), otherDepartment);
        world.Scope.AccessibleDepartments.Add(otherDepartment);
        Assert.True((await world.IssueAsync(join, "JOIN/1", joinTargetDocumentId: principal.DocumentId)).IsSuccess);

        var handler = new GetDocumentNextActionQueryHandler(
            world.Documents,
            world.Decisions,
            new FakeDocumentMovementRepository(),
            new FakeInstitutionSettingRepository(),
            world.Access,
            world.Relations);
        var suggested = await handler.Handle(
            new GetDocumentNextActionQuery(world.Document.DocumentId), CancellationToken.None);

        Assert.True(suggested.IsSuccess, suggested.Error?.Code);
        Assert.Equal(NextActionKind.Move, suggested.Value.Kind);
        Assert.Equal(otherDepartment, suggested.Value.DestinationDepartmentId);
        Assert.Equal(MovementType.ReferralId, suggested.Value.MovementTypeId);

        world.Access.AllowAll = false;
        world.Access.ViewableDocuments.Add(world.Document.DocumentId);
        var hidden = await handler.Handle(
            new GetDocumentNextActionQuery(world.Document.DocumentId), CancellationToken.None);
        Assert.Null(hidden.Value.Kind);
    }
}
