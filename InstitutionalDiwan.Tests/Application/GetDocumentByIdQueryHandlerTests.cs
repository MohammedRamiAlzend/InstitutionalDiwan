using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class GetDocumentByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenDocumentExists_ReturnsMappedResponse()
    {
        var document = Document.CreateDraft("نبذة", "LETTER", "OUTGOING", Guid.NewGuid(), Guid.NewGuid());
        Assert.True(document.IsSuccess);
        var documents = new FakeDocumentRepository();
        documents.Add(document.Value);

        var handler = new GetDocumentByIdQueryHandler(documents);
        var result = await handler.Handle(new GetDocumentByIdQuery(document.Value.DocumentId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(document.Value.DocumentId, result.Value.DocumentId);
        Assert.Equal("نبذة", result.Value.Title);
        Assert.Equal(Document.StatusDraft, result.Value.Status);
    }

    [Fact]
    public async Task Handle_WhenDocumentIsMissing_ReturnsNotFound()
    {
        var handler = new GetDocumentByIdQueryHandler(new FakeDocumentRepository());

        var result = await handler.Handle(new GetDocumentByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DocumentNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
