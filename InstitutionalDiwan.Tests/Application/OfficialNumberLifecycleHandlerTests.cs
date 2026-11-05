using InstitutionalDiwan.Application.Features.Numbering.Numbers;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class OfficialNumberLifecycleHandlerTests
{
    private const int NumberingYear = 2026;

    private sealed class World
    {
        public FakeDocumentRepository Documents { get; } = new();
        public FakeDocumentNumberRepository Numbers { get; } = new();
        public FakeNumberingRegistryRepository Registries { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();

        public Guid UserId { get; } = Guid.NewGuid();
        public Document Document { get; }

        public World(bool seedRegistry = true)
        {
            if (seedRegistry)
            {
                var registry = NumberingRegistry.Create("DEFAULT", "السجل الافتراضي");
                Assert.True(registry.IsSuccess);
                Registries.Add(registry.Value);
            }

            var document = Document.CreateDraft(
                "طلب إحاطة",
                DocumentType.InternalMemoId,
                DocumentDirection.InternalId,
                Guid.NewGuid(),
                UserId);

            Assert.True(document.IsSuccess);
            Document = document.Value;
            Documents.Add(Document);
        }

        public FakeDocumentAccessEvaluator Access { get; } = new() { AllowAll = true };

        public IssueOfficialNumberCommandHandler IssueHandler() =>
            new(Documents, Numbers, Registries, Access, UnitOfWork, TestClock.Instance);

        public CancelOfficialNumberCommandHandler CancelHandler() =>
            new(Documents, Numbers, Access, UnitOfWork);

        public ReplaceOfficialNumberCommandHandler ReplaceHandler() =>
            new(Documents, Numbers, Registries, Access, UnitOfWork, TestClock.Instance);
    }

    private static async Task<DocumentNumberResponse> IssueAsync(World world)
    {
        var result = await world.IssueHandler().Handle(
            new IssueOfficialNumberCommand(world.Document.DocumentId, world.UserId, NumberingYear),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    [Fact]
    public async Task Issue_ForDraft_AllocatesFirstSequenceAndRegisters()
    {
        var world = new World();

        var number = await IssueAsync(world);

        Assert.True(Guid.TryParse(number.OfficialNumberText, out _));
        Assert.Equal(1, number.SequenceValue);
        Assert.Equal(DocumentNumberStatus.ActiveId, number.StatusId);
        Assert.Equal(DocumentStatusType.RegisteredId, world.Document.CurrentStatusId);
        Assert.Single(number.History);
        Assert.Equal(DocumentNumberEventType.IssuedId, number.History.Single().EventTypeId);
    }

    [Fact]
    public async Task Issue_WithoutRegistry_CreatesDefaultRegistry()
    {
        var world = new World(seedRegistry: false);

        var number = await IssueAsync(world);

        Assert.Single(world.Registries.Registries);
        Assert.Equal("DEFAULT", world.Registries.Registries.Single().Code);
        Assert.True(Guid.TryParse(number.OfficialNumberText, out _));
    }

    [Fact]
    public async Task Issue_WhenDocumentIsAlreadyNumbered_ReturnsConflict()
    {
        var world = new World();
        await IssueAsync(world);

        var result = await world.IssueHandler().Handle(
            new IssueOfficialNumberCommand(world.Document.DocumentId, world.UserId, NumberingYear),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.DocumentAlreadyNumbered.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Single(world.Numbers.DocumentNumbers);
    }

    [Fact]
    public async Task Cancel_WhenNumberIsActive_MarksCancelled()
    {
        var world = new World();
        var number = await IssueAsync(world);

        var result = await world.CancelHandler().Handle(
            new CancelOfficialNumberCommand(
                world.Document.DocumentId, number.DocumentNumberId, world.UserId, "خطأ في الترقيم"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentNumberStatus.CancelledId, result.Value.StatusId);
        Assert.Equal("خطأ في الترقيم", result.Value.CancellationReason);
        Assert.Contains(result.Value.History, history =>
            history.EventTypeId == DocumentNumberEventType.CancelledId);
    }

    [Fact]
    public async Task Cancel_WhenNumberAlreadyCancelled_ReturnsConflict()
    {
        var world = new World();
        var number = await IssueAsync(world);

        await world.CancelHandler().Handle(
            new CancelOfficialNumberCommand(
                world.Document.DocumentId, number.DocumentNumberId, world.UserId, "خطأ"),
            CancellationToken.None);

        var result = await world.CancelHandler().Handle(
            new CancelOfficialNumberCommand(
                world.Document.DocumentId, number.DocumentNumberId, world.UserId, "خطأ آخر"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.NumberNotActive.Code, result.Error.Code);
    }

    [Fact]
    public async Task Cancel_WhenNumberBelongsToAnotherDocument_ReturnsNotFound()
    {
        var world = new World();
        var number = await IssueAsync(world);

        var result = await world.CancelHandler().Handle(
            new CancelOfficialNumberCommand(Guid.NewGuid(), number.DocumentNumberId, world.UserId, "سبب"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.NumberNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Replace_LinksOldNumberAndRepointsDocument()
    {
        var world = new World();
        var original = await IssueAsync(world);

        var result = await world.ReplaceHandler().Handle(
            new ReplaceOfficialNumberCommand(
                world.Document.DocumentId,
                original.DocumentNumberId,
                world.UserId,
                "تصحيح رقم الصادر",
                NumberingYear),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(Guid.TryParse(result.Value.OfficialNumberText, out _));
        Assert.Equal(2, result.Value.SequenceValue);

        var replaced = world.Numbers.DocumentNumbers.Single(number =>
            number.DocumentNumberId == original.DocumentNumberId);
        Assert.Equal(DocumentNumberStatus.ReplacedId, replaced.StatusId);
        Assert.Equal(result.Value.DocumentNumberId, replaced.ReplacedByNumberId);

        Assert.Equal(result.Value.DocumentNumberId, world.Document.ActiveOfficialNumberId);
        Assert.Equal(result.Value.OfficialNumberText, world.Document.OfficialNumberText);
    }

    [Fact]
    public async Task Replace_WhenNumberIsNotActive_ReturnsConflict()
    {
        var world = new World();
        var original = await IssueAsync(world);

        await world.CancelHandler().Handle(
            new CancelOfficialNumberCommand(
                world.Document.DocumentId, original.DocumentNumberId, world.UserId, "خطأ"),
            CancellationToken.None);

        var result = await world.ReplaceHandler().Handle(
            new ReplaceOfficialNumberCommand(
                world.Document.DocumentId, original.DocumentNumberId, world.UserId, "تصحيح", NumberingYear),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.NumberNotActive.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetHistory_ReturnsEveryNumberInIssueOrder()
    {
        var world = new World();
        var original = await IssueAsync(world);

        await world.ReplaceHandler().Handle(
            new ReplaceOfficialNumberCommand(
                world.Document.DocumentId, original.DocumentNumberId, world.UserId, "تصحيح", NumberingYear),
            CancellationToken.None);

        var result = await new GetDocumentNumberHistoryQueryHandler(
                world.Documents, world.Numbers, new FakeDocumentAccessEvaluator { AllowAll = true })
            .Handle(new GetDocumentNumberHistoryQuery(world.Document.DocumentId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.True(Guid.TryParse(result.Value.First().OfficialNumberText, out _));
        Assert.NotEqual(result.Value.First().OfficialNumberText, result.Value.Last().OfficialNumberText);
    }

    [Fact]
    public async Task GetHistory_WhenDocumentIsMissing_ReturnsNotFound()
    {
        var world = new World();

        var result = await new GetDocumentNumberHistoryQueryHandler(
                world.Documents, world.Numbers, new FakeDocumentAccessEvaluator { AllowAll = true })
            .Handle(new GetDocumentNumberHistoryQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DocumentNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task NumberWrites_RequireDownloadAccess_AndADeniedCallChangesNothing()
    {
        var world = new World();
        var number = await IssueAsync(world);
        world.Access.AllowAll = false;
        world.Access.ViewableDocuments.Add(world.Document.DocumentId);

        var issue = await world.IssueHandler().Handle(
            new IssueOfficialNumberCommand(world.Document.DocumentId, world.UserId, NumberingYear), CancellationToken.None);
        var cancel = await world.CancelHandler().Handle(
            new CancelOfficialNumberCommand(world.Document.DocumentId, number.DocumentNumberId, world.UserId, "خطأ"), CancellationToken.None);
        var replace = await world.ReplaceHandler().Handle(
            new ReplaceOfficialNumberCommand(world.Document.DocumentId, number.DocumentNumberId, world.UserId, "تصحيح", NumberingYear), CancellationToken.None);

        Assert.Equal(DomainErrors.Correspondence.DocumentAccessDenied.Code, issue.Error.Code);
        Assert.Equal(DomainErrors.Correspondence.DocumentAccessDenied.Code, cancel.Error.Code);
        Assert.Equal(DomainErrors.Correspondence.DocumentAccessDenied.Code, replace.Error.Code);
        Assert.Single(world.Numbers.DocumentNumbers);
        Assert.Equal(DocumentNumberStatus.ActiveId, world.Numbers.DocumentNumbers.Single().StatusId);

        world.Access.DownloadableDocuments.Add(world.Document.DocumentId);
        var allowed = await world.CancelHandler().Handle(
            new CancelOfficialNumberCommand(world.Document.DocumentId, number.DocumentNumberId, world.UserId, "خطأ"), CancellationToken.None);

        Assert.True(allowed.IsSuccess, allowed.Error?.Code);
    }

    [Fact]
    public async Task NumberReads_WithoutViewAccess_AreForbidden()
    {
        var world = new World();
        await IssueAsync(world);
        var denied = new FakeDocumentAccessEvaluator();

        var history = await new GetDocumentNumberHistoryQueryHandler(world.Documents, world.Numbers, denied)
            .Handle(new GetDocumentNumberHistoryQuery(world.Document.DocumentId), CancellationToken.None);
        var entries = await new GetDocumentRegistryNumbersQueryHandler(world.Documents, world.Numbers, denied)
            .Handle(new GetDocumentRegistryNumbersQuery(world.Document.DocumentId), CancellationToken.None);

        Assert.Equal(DomainErrors.Correspondence.DocumentAccessDenied.Code, history.Error.Code);
        Assert.Equal(DomainErrors.Correspondence.DocumentAccessDenied.Code, entries.Error.Code);
    }
}
