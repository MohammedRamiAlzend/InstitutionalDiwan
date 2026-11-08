using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>Field behavior round trip, multi-select filtering and sorting by a field, on a real SQLite database.</summary>
public sealed class DocumentFieldBehaviorPersistenceTests
{
    private static readonly Guid Actor = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly DateTime Now = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    private sealed record Fixture(
        DiwanDbContext Context,
        DocumentTypeSchemaVersion Version,
        DocumentField Tags,
        DocumentField Note);

    private static async Task<Fixture> CreateAsync()
    {
        var context = TestContextFactory.Create();
        context.DocumentDirections.Add(DocumentDirection.Create(DocumentDirection.AllId, "All", "الكل").Value);
        context.DocumentDirections.Add(DocumentDirection.Create(DocumentDirection.InternalId, "Internal", "داخلي").Value);
        context.DocumentStatusTypes.Add(DocumentStatusType.Create(DocumentStatusType.DraftId, "Draft", "مسودة").Value);
        context.DocumentConfidentialityLevels.Add(DocumentConfidentialityLevel.Create(DocumentConfidentialityLevel.NormalId, "Normal", "عادي").Value);
        context.DocumentPriorityLevels.Add(DocumentPriorityLevel.Create(DocumentPriorityLevel.NormalId, "Normal", "عادية").Value);
        context.DocumentTypes.Add(DocumentType.Create(DocumentType.DecisionId, "Decision", "قرار").Value);

        var tags = DocumentField.Create(DocumentType.DecisionId, "tags", DocumentFieldDataType.MultiSelect, Actor).Value;
        var note = DocumentField.Create(DocumentType.DecisionId, "note", DocumentFieldDataType.Text, Actor).Value;
        context.DocumentFields.AddRange(tags, note);

        var version = DocumentTypeSchemaVersion.CreateDraft(DocumentType.DecisionId, 1, Actor).Value;
        Assert.True(version.AddField(tags, DocumentFieldSettings.Create(DocumentFieldDataType.MultiSelect, "وسوم", false, 1,
            options: new[] { new DocumentFieldOption("a_b", "أ"), new DocumentFieldOption("axb", "ب"), new DocumentFieldOption("c", "ج") },
            behavior: new DocumentFieldBehavior(IsFilterable: true, ShowInTable: true, HelpTextAr: "اختر وسمًا")).Value).IsSuccess);
        Assert.True(version.AddField(note, DocumentFieldSettings.Create(DocumentFieldDataType.Text, "ملاحظة", false, 2,
            behavior: new DocumentFieldBehavior(ShowInTable: true)).Value).IsSuccess);
        version.Publish(Actor);
        context.DocumentTypeSchemaVersions.Add(version);

        void Add(string title, params DocumentFieldValueInput[] values)
        {
            var document = Document.CreateDraft(
                title, DocumentType.DecisionId, DocumentDirection.InternalId, Guid.NewGuid(), Actor,
                schemaVersionId: version.SchemaVersionId).Value;
            Assert.True(document.SetFieldValues(version, values).IsSuccess);
            context.Documents.Add(document);
        }

        Add("d1", new DocumentFieldValueInput("tags", "a_b,c"), new DocumentFieldValueInput("note", "b"));
        Add("d2", new DocumentFieldValueInput("tags", "axb"), new DocumentFieldValueInput("note", "c"));
        Add("d3", new DocumentFieldValueInput("note", "a"));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new Fixture(context, version, tags, note);
    }

    private static async Task<string[]> TitlesAsync(
        Fixture fixture,
        IReadOnlyList<DocumentFieldFilter>? filters = null,
        DocumentFieldSort? sort = null,
        bool descending = false)
    {
        var page = await new DocumentRepository(fixture.Context).ListPagedAsync(
            new PageRequest(1, 50, null, "title", descending),
            new DocumentViewAccess(Actor, null, Now),
            documentTypeId: DocumentType.DecisionId,
            fieldFilters: filters,
            fieldSort: sort);
        return page.Items.Select(document => document.Title).ToArray();
    }

    private static DocumentFieldFilter Tag(Fixture fixture, string key) =>
        new(fixture.Tags.DocumentFieldId, DocumentFieldDataType.MultiSelect, FieldFilterOperator.Equals, OptionKey: key);

    [Fact]
    public async Task Behavior_SurvivesSaving()
    {
        var fixture = await CreateAsync();

        var loaded = await fixture.Context.DocumentTypeSchemaVersionFields
            .SingleAsync(field => field.FieldKey == "tags");

        Assert.Equal(new DocumentFieldBehavior(IsFilterable: true, ShowInTable: true, HelpTextAr: "اختر وسمًا"), loaded.Behavior);
    }

    [Fact]
    public async Task MultiSelect_MatchesAWholeChoice_NotAWildcard()
    {
        var fixture = await CreateAsync();

        Assert.Equal(new[] { "d1" }, await TitlesAsync(fixture, new[] { Tag(fixture, "c") }));
        // "a_b" must not match "axb" through the LIKE single-character wildcard.
        Assert.Equal(new[] { "d1" }, await TitlesAsync(fixture, new[] { Tag(fixture, "a_b") }));
        Assert.Equal(new[] { "d2" }, await TitlesAsync(fixture, new[] { Tag(fixture, "axb") }));
        Assert.Empty(await TitlesAsync(fixture, new[] { Tag(fixture, "b") }));
    }

    [Fact]
    public async Task Sort_OrdersByTheFieldValue()
    {
        var fixture = await CreateAsync();
        var sort = new DocumentFieldSort(fixture.Note.DocumentFieldId, DocumentFieldDataType.Text);

        Assert.Equal(new[] { "d3", "d1", "d2" }, await TitlesAsync(fixture, sort: sort));
        Assert.Equal(new[] { "d2", "d1", "d3" }, await TitlesAsync(fixture, sort: sort, descending: true));
    }

    [Fact]
    public async Task ListFieldValues_ReturnsOnlyTheRequestedColumns()
    {
        var fixture = await CreateAsync();
        var repository = new DocumentRepository(fixture.Context);
        var ids = await fixture.Context.Documents.Select(document => document.DocumentId).ToListAsync();

        var values = await repository.ListFieldValuesAsync(ids, new[] { fixture.Tags.DocumentFieldId });

        Assert.Equal(2, values.Count);
        Assert.All(values, value => Assert.Equal(fixture.Tags.DocumentFieldId, value.DocumentFieldId));
    }
}
