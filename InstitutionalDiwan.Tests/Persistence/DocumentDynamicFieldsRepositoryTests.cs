using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>
/// Exercises the dynamic-field tables against a real SQLite database: the JSON options column, the
/// "one draft per type" and "unique key per type" indexes, and replacing a draft's stored values.
/// </summary>
public sealed class DocumentDynamicFieldsRepositoryTests
{
    private static readonly Guid Actor = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static async Task<DiwanDbContext> CreateContextAsync()
    {
        var context = TestContextFactory.Create();

        context.DocumentDirections.Add(DocumentDirection.Create(DocumentDirection.AllId, "All", "الكل").Value);
        context.DocumentDirections.Add(DocumentDirection.Create(DocumentDirection.InternalId, "Internal", "داخلي").Value);
        context.DocumentStatusTypes.Add(DocumentStatusType.Create(DocumentStatusType.DraftId, "Draft", "مسودة").Value);
        context.DocumentConfidentialityLevels.Add(DocumentConfidentialityLevel.Create(DocumentConfidentialityLevel.NormalId, "Normal", "عادي").Value);
        context.DocumentPriorityLevels.Add(DocumentPriorityLevel.Create(DocumentPriorityLevel.NormalId, "Normal", "عادية").Value);
        context.DocumentTypes.Add(DocumentType.Create(DocumentType.DecisionId, "Decision", "قرار").Value);
        await context.SaveChangesAsync();
        return context;
    }

    private static DocumentField Field(string key, DocumentFieldDataType type) =>
        DocumentField.Create(DocumentType.DecisionId, key, type, Actor).Value;

    private static DocumentFieldSettings Settings(
        DocumentFieldDataType type,
        bool required = false,
        IReadOnlyList<DocumentFieldOption>? options = null,
        string? regex = null) =>
        DocumentFieldSettings.Create(type, "تسمية", required, 1, regex, regex is null ? null : "غير صالح", options: options).Value;

    /// <summary>Adds and saves a published version holding a select, a number and a required text field.</summary>
    private static async Task<DocumentTypeSchemaVersion> PublishedVersionAsync(DiwanDbContext context)
    {
        var kind = Field("kind", DocumentFieldDataType.Select);
        var amount = Field("amount", DocumentFieldDataType.Number);
        var note = Field("note", DocumentFieldDataType.Text);
        context.DocumentFields.AddRange(kind, amount, note);

        var version = DocumentTypeSchemaVersion.CreateDraft(DocumentType.DecisionId, 1, Actor).Value;
        version.AddField(kind, Settings(DocumentFieldDataType.Select, options:
            new[] { new DocumentFieldOption("a", "أ"), new DocumentFieldOption("b", "ب") }));
        version.AddField(amount, Settings(DocumentFieldDataType.Number));
        version.AddField(note, Settings(DocumentFieldDataType.Text, required: true));
        version.Publish(Actor);
        context.DocumentTypeSchemaVersions.Add(version);

        await context.SaveChangesAsync();
        return version;
    }

    private static Document NewDocument(Guid schemaVersionId) =>
        Document.CreateDraft(
            "قرار", DocumentType.DecisionId, DocumentDirection.InternalId, Guid.NewGuid(), Actor,
            schemaVersionId: schemaVersionId).Value;

    [Fact]
    public async Task SchemaVersion_RoundTripsFieldsOptionsAndSettings()
    {
        await using var context = await CreateContextAsync();
        var versions = new DocumentTypeSchemaVersionRepository(context);
        var saved = await PublishedVersionAsync(context);
        context.ChangeTracker.Clear();

        var loaded = await versions.GetPublishedAsync(DocumentType.DecisionId);

        Assert.NotNull(loaded);
        Assert.Equal(saved.SchemaVersionId, loaded!.SchemaVersionId);
        Assert.Equal(DocumentTypeSchemaVersionStatus.Published, loaded.Status);
        Assert.Equal(3, loaded.Fields.Count);
        var select = loaded.Fields.Single(f => f.FieldKey == "kind");
        Assert.Equal(DocumentFieldDataType.Select, select.DataType);
        Assert.Equal(new[] { "a", "b" }, select.Options.Select(o => o.Key));
        Assert.Equal("ب", select.Options[1].LabelAr);
        Assert.True(loaded.Fields.Single(f => f.FieldKey == "note").IsRequired);
    }

    [Fact]
    public async Task Repositories_FindDraftPublishedAndNextVersionNumber()
    {
        await using var context = await CreateContextAsync();
        var versions = new DocumentTypeSchemaVersionRepository(context);

        Assert.Equal(0, await versions.GetLatestVersionNumberAsync(DocumentType.DecisionId));
        Assert.Null(await versions.GetDraftAsync(DocumentType.DecisionId));
        Assert.Null(await versions.GetPublishedAsync(DocumentType.DecisionId));

        var published = await PublishedVersionAsync(context);
        var draft = DocumentTypeSchemaVersion.CreateDraftFrom(published, 2, Actor).Value;
        context.DocumentTypeSchemaVersions.Add(draft);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Equal(2, await versions.GetLatestVersionNumberAsync(DocumentType.DecisionId));
        Assert.Equal(draft.SchemaVersionId, (await versions.GetDraftAsync(DocumentType.DecisionId))!.SchemaVersionId);
        Assert.Equal(published.SchemaVersionId, (await versions.GetPublishedAsync(DocumentType.DecisionId))!.SchemaVersionId);
        Assert.Equal(new[] { 2, 1 }, (await versions.ListByDocumentTypeAsync(DocumentType.DecisionId)).Select(v => v.VersionNumber));
    }

    [Fact]
    public async Task SecondDraftForTheSameType_IsRejectedByTheUniqueIndex()
    {
        await using var context = await CreateContextAsync();
        context.DocumentTypeSchemaVersions.Add(DocumentTypeSchemaVersion.CreateDraft(DocumentType.DecisionId, 1, Actor).Value);
        await context.SaveChangesAsync();

        context.DocumentTypeSchemaVersions.Add(DocumentTypeSchemaVersion.CreateDraft(DocumentType.DecisionId, 2, Actor).Value);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task DuplicateFieldKeyForTheSameType_IsRejectedByTheUniqueIndex()
    {
        await using var context = await CreateContextAsync();
        var fields = new DocumentFieldRepository(context);
        fields.Add(Field("session_no", DocumentFieldDataType.Text));
        await context.SaveChangesAsync();

        fields.Add(Field("session_no", DocumentFieldDataType.Number));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        Assert.Equal(DocumentFieldDataType.Text, (await fields.GetByKeyAsync(DocumentType.DecisionId, "session_no"))!.DataType);
    }

    [Fact]
    public async Task DocumentValues_PersistAndAreReplacedOnUpdate()
    {
        await using var context = await CreateContextAsync();
        var documents = new DocumentRepository(context);
        var version = await PublishedVersionAsync(context);

        var document = NewDocument(version.SchemaVersionId);
        Assert.True(document.SetFieldValues(version, new[]
        {
            new DocumentFieldValueInput("kind", "a"),
            new DocumentFieldValueInput("amount", "12.5"),
            new DocumentFieldValueInput("note", "أول"),
        }).IsSuccess);
        context.Documents.Add(document);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var reloaded = await documents.GetByIdAsync(document.DocumentId);
        Assert.Equal(version.SchemaVersionId, reloaded!.SchemaVersionId);
        Assert.Equal(3, reloaded.FieldValues.Count);
        Assert.Contains(reloaded.FieldValues, v => v.OptionKey == "a");
        Assert.Contains(reloaded.FieldValues, v => v.NumberValue == 12.5m);

        // Replace: same fields get new values, one is cleared. EF must delete the old rows without
        // tripping the (document, field) unique index.
        var current = await new DocumentTypeSchemaVersionRepository(context).GetByIdAsync(version.SchemaVersionId);
        Assert.True(reloaded.SetFieldValues(current!, new[]
        {
            new DocumentFieldValueInput("kind", "b"),
            new DocumentFieldValueInput("amount", ""),
            new DocumentFieldValueInput("note", "ثان"),
        }).IsSuccess);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var updated = await documents.GetByIdAsync(document.DocumentId);
        Assert.Equal(2, updated!.FieldValues.Count);
        Assert.Contains(updated.FieldValues, v => v.OptionKey == "b");
        Assert.Contains(updated.FieldValues, v => v.TextValue == "ثان");
        Assert.DoesNotContain(updated.FieldValues, v => v.NumberValue is not null);
    }

    [Fact]
    public async Task PinnedSchemaVersion_CannotBeDeletedWhileADocumentUsesIt()
    {
        await using var context = await CreateContextAsync();
        var version = await PublishedVersionAsync(context);
        context.Documents.Add(NewDocument(version.SchemaVersionId));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Only the version is tracked, so the database's own foreign key must refuse the delete.
        var tracked = await new DocumentTypeSchemaVersionRepository(context).GetByIdAsync(version.SchemaVersionId);
        context.DocumentTypeSchemaVersions.Remove(tracked!);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
