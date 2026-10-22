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

        public IssueOfficialNumberCommandHandler IssueHandler() =>
            new(Documents, Numbers, Registries, UnitOfWork, TestClock.Instance);

        public CancelOfficialNumberCommandHandler CancelHandler() =>
            new(Numbers, UnitOfWork);

        public ReplaceOfficialNumberCommandHandler ReplaceHandler() =>
            new(Documents, Numbers, Registries, UnitOfWork, TestClock.Instance);
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

        Assert.Equal($"{NumberingYear}/000001", number.OfficialNumberText);
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
        Assert.Equal($"{NumberingYear}/000001", number.OfficialNumberText);
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
        Assert.Equal($"{NumberingYear}/000002", result.Value.OfficialNumberText);
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

        var result = await new GetDocumentNumberHistoryQueryHandler(world.Documents, world.Numbers)
            .Handle(new GetDocumentNumberHistoryQuery(world.Document.DocumentId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal($"{NumberingYear}/000001", result.Value.First().OfficialNumberText);
        Assert.Equal($"{NumberingYear}/000002", result.Value.Last().OfficialNumberText);
    }

    [Fact]
    public async Task GetHistory_WhenDocumentIsMissing_ReturnsNotFound()
    {
        var world = new World();

        var result = await new GetDocumentNumberHistoryQueryHandler(world.Documents, world.Numbers)
            .Handle(new GetDocumentNumberHistoryQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DocumentNotFound.Code, result.Error.Code);
    }
}
