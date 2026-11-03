using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

/// <summary>
/// Every single-document read answers 404 for a missing document, 403 for one the caller cannot view,
/// and the document itself once VIEW passes.
/// </summary>
public sealed class DocumentReadAccessHandlerTests
{
    private sealed class World
    {
        public FakeDocumentRepository Documents { get; } = new();
        public FakeDocumentAccessEvaluator Access { get; } = new();
        public Document Document { get; }

        public World()
        {
            Document = Document.CreateDraft(
                "كتاب",
                DocumentType.InternalMemoId,
                DocumentDirection.InternalId,
                Guid.NewGuid(),
                Guid.NewGuid()).Value;
            Documents.Add(Document);
        }

        public Task<Result<DocumentResponse>> GetByIdAsync(Guid documentId) =>
            new GetDocumentByIdQueryHandler(Documents, Access)
                .Handle(new GetDocumentByIdQuery(documentId), CancellationToken.None);

        public Task<Result<DocumentDetailResponse>> GetDetailsAsync(Guid documentId) =>
            new GetDocumentDetailsQueryHandler(Documents, Access)
                .Handle(new GetDocumentDetailsQuery(documentId), CancellationToken.None);

        public Task<Result<IReadOnlyCollection<DocumentTimelineEntryResponse>>> GetTimelineAsync(Guid documentId) =>
            new GetDocumentTimelineQueryHandler(Documents, new FakeDocumentNumberRepository(), Access)
                .Handle(new GetDocumentTimelineQuery(documentId), CancellationToken.None);

        public Task<Result<DocumentFieldsResponse>> GetFieldsAsync(Guid documentId) =>
            new GetDocumentFieldsQueryHandler(Documents, new FakeDocumentTypeSchemaVersionRepository(), Access)
                .Handle(new GetDocumentFieldsQuery(documentId), CancellationToken.None);
    }

    private static void AssertDenied(Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DocumentAccessDenied.Code, result.Error.Code);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    private static void AssertNotFound(Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DocumentNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Reads_WithoutViewAccess_AreForbidden()
    {
        var world = new World();
        var id = world.Document.DocumentId;

        AssertDenied(await world.GetByIdAsync(id));
        AssertDenied(await world.GetDetailsAsync(id));
        AssertDenied(await world.GetTimelineAsync(id));
        AssertDenied(await world.GetFieldsAsync(id));
    }

    [Fact]
    public async Task Reads_WithViewAccess_Succeed()
    {
        var world = new World();
        var id = world.Document.DocumentId;
        world.Access.ViewableDocuments.Add(id);

        Assert.True((await world.GetByIdAsync(id)).IsSuccess);
        Assert.True((await world.GetDetailsAsync(id)).IsSuccess);
        Assert.True((await world.GetTimelineAsync(id)).IsSuccess);
        Assert.True((await world.GetFieldsAsync(id)).IsSuccess);
    }

    [Fact]
    public async Task Reads_OfAMissingDocument_AreNotFound()
    {
        var world = new World();
        var id = Guid.NewGuid();

        AssertNotFound(await world.GetByIdAsync(id));
        AssertNotFound(await world.GetDetailsAsync(id));
        AssertNotFound(await world.GetTimelineAsync(id));
        AssertNotFound(await world.GetFieldsAsync(id));
    }
}
