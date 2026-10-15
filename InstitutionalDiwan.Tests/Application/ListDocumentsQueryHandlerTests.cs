using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ListDocumentsQueryHandlerTests
{
    private static Document CreateDocument(string title)
    {
        var document = Document.CreateDraft(title, "LETTER", "INTERNAL", Guid.NewGuid(), Guid.NewGuid());
        Assert.True(document.IsSuccess);
        return document.Value;
    }

    [Fact]
    public async Task Handle_WithNoDocuments_ReturnsEmptyCollection()
    {
        var handler = new ListDocumentsQueryHandler(new FakeDocumentRepository());

        var result = await handler.Handle(new ListDocumentsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsAllDocumentsWhenUnderTheLimit()
    {
        var documents = new FakeDocumentRepository();
        documents.Add(CreateDocument("first"));
        documents.Add(CreateDocument("second"));

        var handler = new ListDocumentsQueryHandler(documents);
        var result = await handler.Handle(new ListDocumentsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, document => document.Title == "first");
        Assert.Contains(result.Value, document => document.Title == "second");
    }

    [Fact]
    public async Task Handle_CapsResultAtTheDefaultLimit()
    {
        var documents = new FakeDocumentRepository();
        for (var i = 0; i < 51; i++)
            documents.Add(CreateDocument($"doc-{i}"));

        var handler = new ListDocumentsQueryHandler(documents);
        var result = await handler.Handle(new ListDocumentsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value.Count);
    }
}
