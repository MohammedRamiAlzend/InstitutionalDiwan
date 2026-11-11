using InstitutionalDiwan.Application.Features.Decisions;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Decisions;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
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
        }

        public IssueDocumentDecisionCommandHandler IssueHandler() =>
            new(Documents, Types, Decisions, new FakeDocumentFileRepository(), Scope, UnitOfWork, Notifications);

        public Task<InstitutionalDiwan.Domain.Common.Result<DocumentDecisionResponse>> IssueAsync(
            DecisionType type, string number, DateOnly? date = null) =>
            IssueHandler().Handle(
                new IssueDocumentDecisionCommand(
                    Document.DocumentId, UserId, type.DecisionTypeId, number, date ?? DecisionDate, null, null),
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
    public async Task Issue_JoinEffect_IsRefusedUntilUnificationExists()
    {
        var world = new World();
        var type = Type(DecisionEffect.JoinId, appealable: false);
        world.Types.Add(type);

        var issued = await world.IssueAsync(type, "2026/1");

        Assert.Equal(DomainErrors.Decisions.JoinTargetInvalid.Code, issued.Error.Code);
        Assert.Empty(world.Decisions.Decisions);
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
}
