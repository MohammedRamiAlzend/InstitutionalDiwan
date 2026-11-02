using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Tests.Application;

public sealed class DocumentDynamicFieldsDomainTests
{
    private static readonly Guid Actor = Guid.NewGuid();

    private static DocumentField NewField(string key, DocumentFieldDataType type) =>
        DocumentField.Create(DocumentType.DecisionId, key, type, Actor).Value;

    private static DocumentFieldSettings Settings(
        DocumentFieldDataType type,
        bool required = false,
        string? regex = null,
        decimal? min = null,
        decimal? max = null,
        int? maxLength = null,
        IReadOnlyList<DocumentFieldOption>? options = null) =>
        DocumentFieldSettings.Create(type, "تسمية", required, 1, regex, regex is null ? null : "قيمة غير صالحة", min, max, maxLength, options).Value;

    private static DocumentTypeSchemaVersion DraftWith(params (DocumentField Field, DocumentFieldSettings Settings)[] fields)
    {
        var version = DocumentTypeSchemaVersion.CreateDraft(DocumentType.DecisionId, 1, Actor).Value;
        foreach (var (field, settings) in fields)
            Assert.True(version.AddField(field, settings).IsSuccess);
        return version;
    }

    private static Document NewDraftDocument(Guid? schemaVersionId) =>
        Document.CreateDraft(
            "قرار", DocumentType.DecisionId, DocumentDirection.InternalId, Guid.NewGuid(), Actor,
            schemaVersionId: schemaVersionId).Value;

    [Theory]
    [InlineData("session_no", true)]
    [InlineData("a", true)]
    [InlineData("Session", false)]
    [InlineData("1abc", false)]
    [InlineData("has-dash", false)]
    [InlineData("", false)]
    public void DocumentField_Create_ValidatesKey(string key, bool valid)
    {
        var result = DocumentField.Create(DocumentType.DecisionId, key, DocumentFieldDataType.Text, Actor);

        Assert.Equal(valid, result.IsSuccess);
    }

    [Fact]
    public void Settings_RejectPatternOnNumberField_AndInvalidRegex_AndMissingMessage()
    {
        Assert.Equal("DocumentFields.RegexNotAllowed",
            DocumentFieldSettings.Create(DocumentFieldDataType.Number, "x", false, 1, "^a$", "م").Error.Code);
        Assert.Equal("DocumentFields.RegexInvalid",
            DocumentFieldSettings.Create(DocumentFieldDataType.Text, "x", false, 1, "([", "م").Error.Code);
        Assert.Equal("DocumentFields.RegexMessageRequired",
            DocumentFieldSettings.Create(DocumentFieldDataType.Text, "x", false, 1, "^a$", null).Error.Code);
    }

    [Fact]
    public void Settings_SelectNeedsUniqueOptions_AndOtherTypesRejectOptions()
    {
        var none = DocumentFieldSettings.Create(DocumentFieldDataType.Select, "x", false, 1);
        var duplicate = DocumentFieldSettings.Create(DocumentFieldDataType.Select, "x", false, 1, options:
            new[] { new DocumentFieldOption("a", "أ"), new DocumentFieldOption("a", "ب") });
        var onText = DocumentFieldSettings.Create(DocumentFieldDataType.Text, "x", false, 1, options:
            new[] { new DocumentFieldOption("a", "أ") });

        Assert.Equal("DocumentFields.OptionsRequired", none.Error.Code);
        Assert.Equal("DocumentFields.OptionKeyDuplicate", duplicate.Error.Code);
        Assert.Equal("DocumentFields.OptionsNotAllowed", onText.Error.Code);
    }

    [Fact]
    public void BuildValues_ParsesEachDataType()
    {
        var version = DraftWith(
            (NewField("title_note", DocumentFieldDataType.Text), Settings(DocumentFieldDataType.Text)),
            (NewField("amount", DocumentFieldDataType.Number), Settings(DocumentFieldDataType.Number, min: 0, max: 100)),
            (NewField("due", DocumentFieldDataType.Date), Settings(DocumentFieldDataType.Date)),
            (NewField("urgent", DocumentFieldDataType.Boolean), Settings(DocumentFieldDataType.Boolean)),
            (NewField("kind", DocumentFieldDataType.Select), Settings(DocumentFieldDataType.Select, options:
                new[] { new DocumentFieldOption("a", "أ") })));

        var result = version.BuildValues(Guid.NewGuid(), new[]
        {
            new DocumentFieldValueInput("title_note", " نص "),
            new DocumentFieldValueInput("amount", "12.5"),
            new DocumentFieldValueInput("due", "2026-11-02"),
            new DocumentFieldValueInput("urgent", "true"),
            new DocumentFieldValueInput("kind", "a"),
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);
        Assert.Contains(result.Value, v => v.TextValue == "نص");
        Assert.Contains(result.Value, v => v.NumberValue == 12.5m);
        Assert.Contains(result.Value, v => v.DateValue == new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Value, v => v.BoolValue == true);
        Assert.Contains(result.Value, v => v.OptionKey == "a");
    }

    [Fact]
    public void BuildValues_RejectsBadValues()
    {
        var version = DraftWith(
            (NewField("code", DocumentFieldDataType.Text), Settings(DocumentFieldDataType.Text, regex: "^[0-9]{3}$")),
            (NewField("amount", DocumentFieldDataType.Number), Settings(DocumentFieldDataType.Number, min: 0, max: 100)));
        var id = Guid.NewGuid();

        Assert.Equal("DocumentFields.ValuePatternMismatch",
            version.BuildValues(id, new[] { new DocumentFieldValueInput("code", "12a") }).Error.Code);
        Assert.Equal("DocumentFields.ValueAboveMaximum",
            version.BuildValues(id, new[] { new DocumentFieldValueInput("amount", "101") }).Error.Code);
        Assert.Equal("DocumentFields.ValueTypeInvalid",
            version.BuildValues(id, new[] { new DocumentFieldValueInput("amount", "abc") }).Error.Code);
        Assert.Equal("DocumentFields.UnknownField",
            version.BuildValues(id, new[] { new DocumentFieldValueInput("missing", "x") }).Error.Code);
        Assert.Equal("DocumentFields.DuplicateValue",
            version.BuildValues(id, new[] { new DocumentFieldValueInput("amount", "1"), new DocumentFieldValueInput("amount", "2") }).Error.Code);
    }

    [Fact]
    public void BuildValues_CatastrophicPattern_IsStoppedByTimeout()
    {
        var version = DraftWith(
            (NewField("evil", DocumentFieldDataType.Text), Settings(DocumentFieldDataType.Text, regex: "^(a+)+$")));

        var result = version.BuildValues(Guid.NewGuid(), new[]
        {
            new DocumentFieldValueInput("evil", new string('a', 60) + "!")
        });

        Assert.True(result.IsFailure);
        Assert.Equal("DocumentFields.RegexTimedOut", result.Error.Code);
    }

    [Fact]
    public void PublishedVersion_IsFrozen_AndRaisesEvent()
    {
        var version = DraftWith((NewField("a", DocumentFieldDataType.Text), Settings(DocumentFieldDataType.Text)));

        Assert.True(version.Publish(Actor).IsSuccess);
        Assert.Single(version.DomainEvents);

        Assert.Equal("DocumentFields.SchemaVersionNotDraft",
            version.AddField(NewField("b", DocumentFieldDataType.Text), Settings(DocumentFieldDataType.Text)).Error.Code);
        Assert.Equal("DocumentFields.SchemaVersionNotDraft", version.Publish(Actor).Error.Code);
    }

    [Fact]
    public void UpdateField_CannotChangeDataType_AndCreateDraftFromKeepsFieldIdentity()
    {
        var field = NewField("a", DocumentFieldDataType.Text);
        var version = DraftWith((field, Settings(DocumentFieldDataType.Text)));

        Assert.Equal("DocumentFields.FieldTypeMismatch",
            version.UpdateField(field.DocumentFieldId, Settings(DocumentFieldDataType.Number)).Error.Code);

        version.Publish(Actor);
        var next = DocumentTypeSchemaVersion.CreateDraftFrom(version, 2, Actor).Value;

        Assert.Equal(DocumentTypeSchemaVersionStatus.Draft, next.Status);
        Assert.Equal(2, next.VersionNumber);
        var copied = Assert.Single(next.Fields);
        Assert.Equal(field.DocumentFieldId, copied.DocumentFieldId);
        Assert.NotEqual(version.Fields.Single().SchemaVersionFieldId, copied.SchemaVersionFieldId);
    }

    [Fact]
    public void Document_SetFieldValues_UsesPinnedVersion_AndLocksAfterRegistration()
    {
        var version = DraftWith((NewField("a", DocumentFieldDataType.Text), Settings(DocumentFieldDataType.Text, required: true)));
        version.Publish(Actor);
        var other = DraftWith((NewField("a", DocumentFieldDataType.Text), Settings(DocumentFieldDataType.Text)));
        var document = NewDraftDocument(version.SchemaVersionId);

        Assert.Equal("DocumentFields.SchemaVersionMismatch",
            document.SetFieldValues(other, new[] { new DocumentFieldValueInput("a", "x") }).Error.Code);

        Assert.Equal("DocumentFields.ValueRequired", document.EnsureRequiredFieldsComplete(version).Error.Code);
        Assert.True(document.SetFieldValues(version, new[] { new DocumentFieldValueInput("a", "x") }).IsSuccess);
        Assert.True(document.EnsureRequiredFieldsComplete(version).IsSuccess);
        Assert.Single(document.FieldValues);

        // Clearing a value removes it.
        Assert.True(document.SetFieldValues(version, new[] { new DocumentFieldValueInput("a", "  ") }).IsSuccess);
        Assert.Empty(document.FieldValues);
    }

    [Fact]
    public void Document_WithoutPinnedSchema_HasNothingToCheck_AndRejectsValues()
    {
        var version = DraftWith((NewField("a", DocumentFieldDataType.Text), Settings(DocumentFieldDataType.Text)));
        var document = NewDraftDocument(schemaVersionId: null);

        Assert.True(document.EnsureRequiredFieldsComplete(null).IsSuccess);
        Assert.Equal("DocumentFields.SchemaVersionMismatch",
            document.SetFieldValues(version, new[] { new DocumentFieldValueInput("a", "x") }).Error.Code);
    }
}
