using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ListDocumentsQueryHandlerTests
{
    private static Document CreateDocument(string title)
    {
        var document = Document.CreateDraft(title, DocumentType.OutgoingLetterId, DocumentDirection.InternalId, Guid.NewGuid(), Guid.NewGuid());
        Assert.True(document.IsSuccess);
        return document.Value;
    }

    /// <summary>An institution-wide caller by default, so these tests stay about paging and filters.</summary>
    private static ListDocumentsQueryHandler CreateHandler(
        FakeDocumentRepository documents,
        FakeDepartmentScopeEvaluator? scope = null,
        Guid? callerId = null) =>
        new(
            documents,
            new FakeDocumentFieldRepository(),
            scope ?? new FakeDepartmentScopeEvaluator { InstitutionWide = true },
            new FakeUserContext { IsAuthenticated = true, UserId = callerId ?? Guid.NewGuid() },
            TestClock.Instance);

    [Fact]
    public async Task Handle_PassesTheCallerScopeAndClockToTheQuery()
    {
        var documents = new FakeDocumentRepository();
        var callerId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var scope = new FakeDepartmentScopeEvaluator();
        scope.AccessibleDepartments.Add(departmentId);

        await CreateHandler(documents, scope, callerId).Handle(new ListDocumentsQuery(), CancellationToken.None);

        var access = documents.LastViewAccess!;
        Assert.Equal(callerId, access.CallerUserId);
        Assert.Equal(new[] { departmentId }, access.AccessibleDepartmentIds);
        Assert.Equal(TestClock.Instance.UtcNow, access.AsOfUtc);
    }

    [Fact]
    public async Task Handle_FilterOnADepartmentOutsideTheScope_ReturnsNothing()
    {
        var documents = new FakeDocumentRepository();
        var reachable = Guid.NewGuid();
        var unreachable = Guid.NewGuid();
        documents.Add(Document.CreateDraft("reachable", DocumentType.OutgoingLetterId, DocumentDirection.InternalId, reachable, Guid.NewGuid()).Value);
        documents.Add(Document.CreateDraft("unreachable", DocumentType.OutgoingLetterId, DocumentDirection.InternalId, unreachable, Guid.NewGuid()).Value);
        var scope = new FakeDepartmentScopeEvaluator();
        scope.AccessibleDepartments.Add(reachable);

        var handler = CreateHandler(documents, scope);

        var filtered = await handler.Handle(new ListDocumentsQuery(OwningDepartmentId: unreachable), CancellationToken.None);
        var all = await handler.Handle(new ListDocumentsQuery(), CancellationToken.None);

        Assert.Empty(filtered.Value.Items);
        Assert.Equal(0, filtered.Value.TotalCount);
        Assert.Equal("reachable", Assert.Single(all.Value.Items).Title);
        Assert.Equal(1, all.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_WithNoDocuments_ReturnsEmptyPage()
    {
        var handler = CreateHandler(new FakeDocumentRepository());

        var result = await handler.Handle(new ListDocumentsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(0, result.Value.TotalPages);
        Assert.False(result.Value.HasNext);
    }

    [Fact]
    public async Task Handle_ReturnsAllDocumentsWhenTheyFitOnOnePage()
    {
        var documents = new FakeDocumentRepository();
        documents.Add(CreateDocument("first"));
        documents.Add(CreateDocument("second"));

        var handler = CreateHandler(documents);
        var result = await handler.Handle(new ListDocumentsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Contains(result.Value.Items, document => document.Title == "first");
        Assert.Contains(result.Value.Items, document => document.Title == "second");
    }

    [Fact]
    public async Task Handle_ReturnsOnePageAndTheTotalCount()
    {
        var documents = new FakeDocumentRepository();
        for (var i = 0; i < 51; i++)
            documents.Add(CreateDocument($"doc-{i}"));

        var handler = CreateHandler(documents);
        var result = await handler.Handle(
            new ListDocumentsQuery(Page: 2, PageSize: 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value.Items.Count);
        Assert.Equal(51, result.Value.TotalCount);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.Equal(2, result.Value.Page);
        Assert.Equal(20, result.Value.PageSize);
        Assert.True(result.Value.HasPrevious);
        Assert.True(result.Value.HasNext);
    }

    [Fact]
    public async Task Handle_ClampsAnOversizedPageSize()
    {
        var documents = new FakeDocumentRepository();
        for (var i = 0; i < 5; i++)
            documents.Add(CreateDocument($"doc-{i}"));

        var handler = CreateHandler(documents);
        var result = await handler.Handle(
            new ListDocumentsQuery(PageSize: 10_000), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value.PageSize);
    }

    [Fact]
    public async Task Handle_SearchesTheTitleAndOfficialNumber()
    {
        var documents = new FakeDocumentRepository();
        documents.Add(CreateDocument("Budget report"));
        documents.Add(CreateDocument("Staff memo"));

        var handler = CreateHandler(documents);
        var result = await handler.Handle(
            new ListDocumentsQuery(Search: "budget"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var document = Assert.Single(result.Value.Items);
        Assert.Equal("Budget report", document.Title);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_FiltersByStatusAndOwningDepartment()
    {
        var documents = new FakeDocumentRepository();
        var departmentId = Guid.NewGuid();

        var matching = Document.CreateDraft(
            "matching", DocumentType.OutgoingLetterId, DocumentDirection.InternalId,
            departmentId, Guid.NewGuid()).Value;
        var otherDepartment = Document.CreateDraft(
            "other", DocumentType.OutgoingLetterId, DocumentDirection.InternalId,
            Guid.NewGuid(), Guid.NewGuid()).Value;

        documents.Add(matching);
        documents.Add(otherDepartment);

        var handler = CreateHandler(documents);
        var result = await handler.Handle(
            new ListDocumentsQuery(OwningDepartmentId: departmentId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var document = Assert.Single(result.Value.Items);
        Assert.Equal("matching", document.Title);
    }

    [Fact]
    public async Task Handle_SortsByTitleWhenRequested()
    {
        var documents = new FakeDocumentRepository();
        documents.Add(CreateDocument("b"));
        documents.Add(CreateDocument("a"));

        var handler = CreateHandler(documents);
        var result = await handler.Handle(
            new ListDocumentsQuery(SortBy: "title", SortDescending: false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "a", "b" }, result.Value.Items.Select(document => document.Title));
    }
}
