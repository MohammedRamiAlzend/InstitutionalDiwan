using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>
/// Registry filtering by dynamic field values against a real SQLite database: every data type and
/// operator, AND between filters, documents without a value, LIKE escaping, and visibility.
/// </summary>
public sealed class DocumentRegistryFieldFilterTests
{
    private static readonly Guid Actor = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly DateTime Now = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    private sealed record Fixture(
        DiwanDbContext Context,
        DocumentField Kind,
        DocumentField Amount,
        DocumentField Note,
        DocumentField Due,
        DocumentField Urgent);

    private static async Task<Fixture> CreateAsync()
    {
        var context = TestContextFactory.Create();
        context.DocumentDirections.Add(DocumentDirection.Create(DocumentDirection.AllId, "All", "الكل").Value);
        context.DocumentDirections.Add(DocumentDirection.Create(DocumentDirection.InternalId, "Internal", "داخلي").Value);
        context.DocumentStatusTypes.Add(DocumentStatusType.Create(DocumentStatusType.DraftId, "Draft", "مسودة").Value);
        context.DocumentConfidentialityLevels.Add(DocumentConfidentialityLevel.Create(DocumentConfidentialityLevel.NormalId, "Normal", "عادي").Value);
        context.DocumentPriorityLevels.Add(DocumentPriorityLevel.Create(DocumentPriorityLevel.NormalId, "Normal", "عادية").Value);
        context.DocumentTypes.Add(DocumentType.Create(DocumentType.DecisionId, "Decision", "قرار").Value);

        DocumentField Field(string key, DocumentFieldDataType type) =>
            DocumentField.Create(DocumentType.DecisionId, key, type, Actor).Value;
        DocumentFieldSettings Settings(DocumentFieldDataType type, IReadOnlyList<DocumentFieldOption>? options = null) =>
            DocumentFieldSettings.Create(type, "تسمية", false, 1, null, null, options: options).Value;

        var kind = Field("kind", DocumentFieldDataType.Select);
        var amount = Field("amount", DocumentFieldDataType.Number);
        var note = Field("note", DocumentFieldDataType.Text);
        var due = Field("due", DocumentFieldDataType.Date);
        var urgent = Field("urgent", DocumentFieldDataType.Boolean);
        context.DocumentFields.AddRange(kind, amount, note, due, urgent);

        var version = DocumentTypeSchemaVersion.CreateDraft(DocumentType.DecisionId, 1, Actor).Value;
        version.AddField(kind, Settings(DocumentFieldDataType.Select,
            new[] { new DocumentFieldOption("a", "أ"), new DocumentFieldOption("b", "ب") }));
        version.AddField(amount, Settings(DocumentFieldDataType.Number));
        version.AddField(note, Settings(DocumentFieldDataType.Text));
        version.AddField(due, Settings(DocumentFieldDataType.Date));
        version.AddField(urgent, Settings(DocumentFieldDataType.Boolean));
        version.Publish(Actor);
        context.DocumentTypeSchemaVersions.Add(version);

        void Add(string title, params DocumentFieldValueInput[] values)
        {
            var document = Document.CreateDraft(
                title, DocumentType.DecisionId, DocumentDirection.InternalId, Guid.NewGuid(), Actor,
                schemaVersionId: version.SchemaVersionId).Value;
            if (values.Length > 0)
                Assert.True(document.SetFieldValues(version, values).IsSuccess);
            context.Documents.Add(document);
        }

        Add("d1",
            new DocumentFieldValueInput("kind", "a"),
            new DocumentFieldValueInput("amount", "10"),
            new DocumentFieldValueInput("note", "عقد A-12_x 100%"),
            new DocumentFieldValueInput("due", "2026-03-10T14:00:00Z"),
            new DocumentFieldValueInput("urgent", "true"));
        Add("d2",
            new DocumentFieldValueInput("kind", "b"),
            new DocumentFieldValueInput("amount", "20.5"),
            new DocumentFieldValueInput("note", "عقد B-7"),
            new DocumentFieldValueInput("due", "2026-03-11T09:00:00Z"),
            new DocumentFieldValueInput("urgent", "false"));
        Add("d3");

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new Fixture(context, kind, amount, note, due, urgent);
    }

    private static async Task<string[]> TitlesAsync(
        Fixture fixture,
        DocumentViewAccess? access = null,
        params DocumentFieldFilter[] filters)
    {
        var page = await new DocumentRepository(fixture.Context).ListPagedAsync(
            new PageRequest(1, 50, null, "title", false),
            access ?? new DocumentViewAccess(Actor, null, Now),
            documentTypeId: DocumentType.DecisionId,
            fieldFilters: filters);
        return page.Items.Select(document => document.Title).ToArray();
    }

    private static Task<string[]> TitlesAsync(Fixture fixture, params DocumentFieldFilter[] filters) =>
        TitlesAsync(fixture, null, filters);

    private static DocumentFieldFilter Number(DocumentField field, FieldFilterOperator op, decimal value) =>
        new(field.DocumentFieldId, DocumentFieldDataType.Number, op, Number: value);

    private static DocumentFieldFilter Text(DocumentField field, FieldFilterOperator op, string value) =>
        new(field.DocumentFieldId, DocumentFieldDataType.Text, op, Text: value);

    private static DocumentFieldFilter Date(DocumentField field, FieldFilterOperator op, string iso) =>
        new(field.DocumentFieldId, DocumentFieldDataType.Date, op, Date: DateTime.Parse(iso, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal));

    [Fact]
    public async Task NoFilters_ReturnsEveryViewableDocument()
    {
        var f = await CreateAsync();
        Assert.Equal(new[] { "d1", "d2", "d3" }, await TitlesAsync(f));
    }

    [Theory]
    [InlineData(FieldFilterOperator.Equals, 20.5, new[] { "d2" })]
    [InlineData(FieldFilterOperator.GreaterThan, 10, new[] { "d2" })]
    [InlineData(FieldFilterOperator.GreaterThanOrEqual, 10, new[] { "d1", "d2" })]
    [InlineData(FieldFilterOperator.LessThan, 20.5, new[] { "d1" })]
    [InlineData(FieldFilterOperator.LessThanOrEqual, 9, new string[0])]
    public async Task Number_Operators(FieldFilterOperator op, double value, string[] expected)
    {
        var f = await CreateAsync();
        Assert.Equal(expected, await TitlesAsync(f, Number(f.Amount, op, (decimal)value)));
    }

    [Fact]
    public async Task Text_ContainsTreatsPercentAndUnderscoreAsLiterals()
    {
        var f = await CreateAsync();

        Assert.Equal(new[] { "d1" }, await TitlesAsync(f, Text(f.Note, FieldFilterOperator.Contains, "A-12")));
        Assert.Equal(new[] { "d1" }, await TitlesAsync(f, Text(f.Note, FieldFilterOperator.Contains, "100%")));
        Assert.Equal(new[] { "d1" }, await TitlesAsync(f, Text(f.Note, FieldFilterOperator.Contains, "12_x")));
        // A bare underscore must not act as a single-character wildcard: only d1 contains one.
        Assert.Equal(new[] { "d1" }, await TitlesAsync(f, Text(f.Note, FieldFilterOperator.Contains, "_")));
        Assert.Empty(await TitlesAsync(f, Text(f.Note, FieldFilterOperator.Contains, "%%")));
    }

    [Fact]
    public async Task Text_Equals_MatchesTheWholeValue()
    {
        var f = await CreateAsync();

        Assert.Equal(new[] { "d2" }, await TitlesAsync(f, Text(f.Note, FieldFilterOperator.Equals, "عقد B-7")));
        Assert.Empty(await TitlesAsync(f, Text(f.Note, FieldFilterOperator.Equals, "عقد")));
    }

    [Fact]
    public async Task Select_And_Boolean_Equality()
    {
        var f = await CreateAsync();

        Assert.Equal(new[] { "d1" }, await TitlesAsync(f,
            new DocumentFieldFilter(f.Kind.DocumentFieldId, DocumentFieldDataType.Select, FieldFilterOperator.Equals, OptionKey: "a")));
        Assert.Equal(new[] { "d2" }, await TitlesAsync(f,
            new DocumentFieldFilter(f.Urgent.DocumentFieldId, DocumentFieldDataType.Boolean, FieldFilterOperator.Equals, Boolean: false)));
        Assert.Empty(await TitlesAsync(f,
            new DocumentFieldFilter(f.Kind.DocumentFieldId, DocumentFieldDataType.Select, FieldFilterOperator.Equals, OptionKey: "zzz")));
    }

    [Theory]
    [InlineData(FieldFilterOperator.Equals, "2026-03-10", new[] { "d1" })]
    [InlineData(FieldFilterOperator.GreaterThan, "2026-03-10", new[] { "d2" })]
    [InlineData(FieldFilterOperator.GreaterThanOrEqual, "2026-03-10", new[] { "d1", "d2" })]
    [InlineData(FieldFilterOperator.LessThan, "2026-03-10", new string[0])]
    [InlineData(FieldFilterOperator.LessThanOrEqual, "2026-03-10", new[] { "d1" })]
    public async Task Date_ComparesByCalendarDay(FieldFilterOperator op, string iso, string[] expected)
    {
        var f = await CreateAsync();
        Assert.Equal(expected, await TitlesAsync(f, Date(f.Due, op, iso)));
    }

    [Fact]
    public async Task SeveralFilters_AreAnded_AndDocumentsWithoutAValueNeverMatch()
    {
        var f = await CreateAsync();

        var selectA = new DocumentFieldFilter(f.Kind.DocumentFieldId, DocumentFieldDataType.Select, FieldFilterOperator.Equals, OptionKey: "a");

        Assert.Equal(new[] { "d1" }, await TitlesAsync(f, selectA, Number(f.Amount, FieldFilterOperator.GreaterThanOrEqual, 10)));
        Assert.Empty(await TitlesAsync(f, selectA, Number(f.Amount, FieldFilterOperator.GreaterThan, 15)));
        // d3 has no stored values, so even a permissive range excludes it.
        Assert.DoesNotContain("d3", await TitlesAsync(f, Number(f.Amount, FieldFilterOperator.GreaterThanOrEqual, 0)));
    }

    [Fact]
    public async Task Filters_NeverWidenWhatTheCallerMayView()
    {
        var f = await CreateAsync();
        var stranger = new DocumentViewAccess(Guid.NewGuid(), new HashSet<Guid>(), Now);

        Assert.Empty(await TitlesAsync(f, stranger, Number(f.Amount, FieldFilterOperator.GreaterThanOrEqual, 0)));
    }
}
