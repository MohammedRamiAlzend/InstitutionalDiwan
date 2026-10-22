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

    [Fact]
    public async Task Handle_WithNoDocuments_ReturnsEmptyPage()
    {
        var handler = new ListDocumentsQueryHandler(new FakeDocumentRepository());

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

        var handler = new ListDocumentsQueryHandler(documents);
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

        var handler = new ListDocumentsQueryHandler(documents);
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

        var handler = new ListDocumentsQueryHandler(documents);
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

        var handler = new ListDocumentsQueryHandler(documents);
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

        var handler = new ListDocumentsQueryHandler(documents);
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

        var handler = new ListDocumentsQueryHandler(documents);
        var result = await handler.Handle(
            new ListDocumentsQuery(SortBy: "title", SortDescending: false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "a", "b" }, result.Value.Items.Select(document => document.Title));
    }
}
