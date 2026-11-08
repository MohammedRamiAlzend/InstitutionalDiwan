using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

/// <summary>Parsing and validation of the registry's <c>fieldFilters</c> parameters.</summary>
public sealed class ListDocumentsFieldFilterTests
{
    private static readonly Guid Actor = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static (ListDocumentsQueryHandler Handler, FakeDocumentRepository Documents) Create()
    {
        var fields = new FakeDocumentFieldRepository();
        fields.Fields.Add(DocumentField.Create(DocumentType.DecisionId, "amount", DocumentFieldDataType.Number, Actor).Value);
        fields.Fields.Add(DocumentField.Create(DocumentType.DecisionId, "note", DocumentFieldDataType.Text, Actor).Value);
        fields.Fields.Add(DocumentField.Create(DocumentType.DecisionId, "due", DocumentFieldDataType.Date, Actor).Value);
        fields.Fields.Add(DocumentField.Create(DocumentType.DecisionId, "urgent", DocumentFieldDataType.Boolean, Actor).Value);
        fields.Fields.Add(DocumentField.Create(DocumentType.DecisionId, "kind", DocumentFieldDataType.Select, Actor).Value);

        var documents = new FakeDocumentRepository();
        var handler = new ListDocumentsQueryHandler(
            documents,
            fields,
            new FakeDocumentTypeSchemaVersionRepository(),
            new FakeDepartmentScopeEvaluator { InstitutionWide = true },
            new FakeUserContext { IsAuthenticated = true, UserId = Guid.NewGuid() },
            TestClock.Instance);
        return (handler, documents);
    }

    private static Task<InstitutionalDiwan.Domain.Common.Result<InstitutionalDiwan.Application.Common.PagedResult<DocumentResponse>>> Run(
        ListDocumentsQueryHandler handler, int? type, params string[] filters) =>
        handler.Handle(new ListDocumentsQuery(DocumentTypeId: type, FieldFilters: filters), CancellationToken.None);

    [Fact]
    public async Task WithoutADocumentType_IsRejected()
    {
        var (handler, _) = Create();

        var result = await Run(handler, null, "amount:gt:5");

        Assert.Equal("DocumentFields.FilterRequiresDocumentType", result.Error.Code);
    }

    [Theory]
    [InlineData("amount")]
    [InlineData("amount:gt")]
    [InlineData(":gt:5")]
    [InlineData("amount:between:5")]
    public async Task MalformedFilter_IsRejected(string filter)
    {
        var (handler, _) = Create();

        var result = await Run(handler, DocumentType.DecisionId, filter);

        Assert.Equal("DocumentFields.FilterMalformed", result.Error.Code);
    }

    [Fact]
    public async Task UnknownField_IsRejected()
    {
        var (handler, _) = Create();

        var result = await Run(handler, DocumentType.DecisionId, "missing:eq:1");

        Assert.Equal("DocumentFields.FilterFieldNotFound", result.Error.Code);
    }

    [Theory]
    [InlineData("amount:contains:5")]
    [InlineData("note:gt:abc")]
    [InlineData("urgent:gt:true")]
    [InlineData("kind:contains:a")]
    [InlineData("due:contains:2026")]
    public async Task OperatorThatDoesNotFitTheDataType_IsRejected(string filter)
    {
        var (handler, _) = Create();

        var result = await Run(handler, DocumentType.DecisionId, filter);

        Assert.Equal("DocumentFields.FilterOperatorNotSupported", result.Error.Code);
    }

    [Theory]
    [InlineData("amount:gt:abc")]
    [InlineData("due:eq:not-a-date")]
    [InlineData("urgent:eq:maybe")]
    [InlineData("note:eq: ")]
    public async Task ValueThatDoesNotFitTheDataType_IsRejected(string filter)
    {
        var (handler, _) = Create();

        var result = await Run(handler, DocumentType.DecisionId, filter);

        Assert.Equal("DocumentFields.FilterValueInvalid", result.Error.Code);
    }

    [Fact]
    public async Task MoreThanFiveFilters_AreRejected()
    {
        var (handler, _) = Create();

        var result = await Run(handler, DocumentType.DecisionId, Enumerable.Repeat("amount:gt:1", 6).ToArray());

        Assert.Equal("DocumentFields.TooManyFilters", result.Error.Code);
    }

    [Fact]
    public async Task ValidFilters_IncludingColonsInTheValue_AreAccepted()
    {
        var (handler, _) = Create();

        var result = await Run(
            handler,
            DocumentType.DecisionId,
            "due:gte:2026-03-10T14:00:00Z",
            "amount:lte:99.5",
            "note:contains:a:b",
            "urgent:eq:true",
            "kind:eq:a");

        Assert.True(result.IsSuccess);
    }
}
