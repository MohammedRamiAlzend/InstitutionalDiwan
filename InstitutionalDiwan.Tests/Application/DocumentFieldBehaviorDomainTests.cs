using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Tests.Application;

/// <summary>Default values, read-only fields, conditional visibility, extra limits, multi-selects and option sources.</summary>
public sealed class DocumentFieldBehaviorDomainTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DocumentFieldOption[] Kinds =
        { new("loan", "إعارة"), new("transfer", "نقل"), new("other", "أخرى") };

    private static DocumentField Field(string key, DocumentFieldDataType type) =>
        DocumentField.Create(DocumentType.DecisionId, key, type, Actor).Value;

    private static DocumentFieldSettings Settings(
        DocumentFieldDataType type,
        DocumentFieldBehavior? behavior = null,
        bool required = false,
        IReadOnlyList<DocumentFieldOption>? options = null,
        int? maxLength = null,
        decimal? min = null) =>
        DocumentFieldSettings.Create(type, "تسمية", required, 1, null, null, min, null, maxLength, options, behavior).Value;

    private static Error ErrorOf(
        DocumentFieldDataType type,
        DocumentFieldBehavior behavior,
        IReadOnlyList<DocumentFieldOption>? options = null,
        decimal? min = null) =>
        DocumentFieldSettings.Create(type, "تسمية", false, 1, null, null, min, null, null, options, behavior).Error;

    private static DocumentTypeSchemaVersion Draft(params (DocumentField Field, DocumentFieldSettings Settings)[] fields)
    {
        var version = DocumentTypeSchemaVersion.CreateDraft(DocumentType.DecisionId, 1, Actor).Value;
        foreach (var (field, settings) in fields)
            Assert.True(version.AddField(field, settings).IsSuccess);
        return version;
    }

    private static DocumentFieldValueInput In(string key, string? value) => new(key, value);

    [Fact]
    public void ReadOnly_NeedsADefault()
    {
        Assert.Equal("DocumentFields.ReadOnlyRequiresDefault",
            ErrorOf(DocumentFieldDataType.Text, new DocumentFieldBehavior(IsReadOnly: true)).Code);
    }

    [Fact]
    public void Default_MustPassTheFieldsOwnRules()
    {
        Assert.Equal("DocumentFields.DefaultValueInvalid",
            ErrorOf(DocumentFieldDataType.Number, new DocumentFieldBehavior(DefaultValue: "abc")).Code);
        Assert.Equal("DocumentFields.DefaultValueInvalid",
            ErrorOf(DocumentFieldDataType.Number, new DocumentFieldBehavior(DefaultValue: "1"), min: 5).Code);
        Assert.Equal("DocumentFields.DefaultValueInvalid",
            ErrorOf(DocumentFieldDataType.Select, new DocumentFieldBehavior(DefaultValue: "nope"), Kinds).Code);
        Assert.Equal("DocumentFields.DefaultValueInvalid",
            ErrorOf(DocumentFieldDataType.Text, new DocumentFieldBehavior(DefaultValue: DocumentFieldBehavior.TodayToken)).Code);
        Assert.True(DocumentFieldSettings.Create(DocumentFieldDataType.Date, "تاريخ", false, 1,
            behavior: new DocumentFieldBehavior(DefaultValue: DocumentFieldBehavior.TodayToken)).IsSuccess);
        Assert.True(DocumentFieldSettings.Create(DocumentFieldDataType.MultiSelect, "أنواع", false, 1, options: Kinds,
            behavior: new DocumentFieldBehavior(DefaultValue: "loan,other")).IsSuccess);
    }

    [Fact]
    public void ReadOnly_IsWrittenByTheServer_AndRefusesAnotherValue()
    {
        var code = Field("code", DocumentFieldDataType.Text);
        var version = Draft((code, Settings(DocumentFieldDataType.Text,
            new DocumentFieldBehavior(DefaultValue: "FIXED", IsReadOnly: true))));
        var id = Guid.NewGuid();

        var empty = version.BuildValues(id, Array.Empty<DocumentFieldValueInput>());
        Assert.Equal("FIXED", Assert.Single(empty.Value).TextValue);

        var same = version.BuildValues(id, new[] { In("code", "FIXED") });
        Assert.Equal("FIXED", Assert.Single(same.Value).TextValue);

        Assert.Equal("DocumentFields.FieldReadOnly",
            version.BuildValues(id, new[] { In("code", "HACK") }).Error.Code);
    }

    [Fact]
    public void ReadOnly_Today_KeepsTheOriginalDateOnLaterSaves()
    {
        var day = Field("received", DocumentFieldDataType.Date);
        var version = Draft((day, Settings(DocumentFieldDataType.Date,
            new DocumentFieldBehavior(DefaultValue: DocumentFieldBehavior.TodayToken, IsReadOnly: true))));
        var id = Guid.NewGuid();

        var first = version.BuildValues(id, Array.Empty<DocumentFieldValueInput>(), utcNow: Now);
        Assert.Equal(Now.Date, Assert.Single(first.Value).DateValue);

        var later = version.BuildValues(id, Array.Empty<DocumentFieldValueInput>(), first.Value, utcNow: Now.AddDays(5));
        Assert.Equal(Now.Date, Assert.Single(later.Value).DateValue);
    }

    [Fact]
    public void DocumentCreatedWithoutInputs_StillGetsReadOnlyValues()
    {
        var code = Field("code", DocumentFieldDataType.Text);
        var version = Draft((code, Settings(DocumentFieldDataType.Text,
            new DocumentFieldBehavior(DefaultValue: "FIXED", IsReadOnly: true))));
        version.Publish(Actor);
        var document = Document.CreateDraft("قرار", DocumentType.DecisionId, DocumentDirection.InternalId,
            Guid.NewGuid(), Actor, schemaVersionId: version.SchemaVersionId).Value;

        Assert.True(document.AddMissingFixedFieldValues(version, Now).IsSuccess);

        Assert.Equal("FIXED", Assert.Single(document.FieldValues).TextValue);
    }

    [Fact]
    public void Visibility_HiddenFieldsAreNotStoredOrRequired()
    {
        var kind = Field("kind", DocumentFieldDataType.Select);
        var reason = Field("reason", DocumentFieldDataType.Text);
        var version = Draft(
            (kind, Settings(DocumentFieldDataType.Select, options: Kinds)),
            (reason, Settings(DocumentFieldDataType.Text,
                new DocumentFieldBehavior(VisibleWhenFieldKey: "kind", VisibleWhenValue: "other"), required: true)));
        var id = Guid.NewGuid();

        var hidden = version.BuildValues(id, new[] { In("kind", "loan"), In("reason", "ignored") }).Value;
        Assert.Equal(kind.DocumentFieldId, Assert.Single(hidden).DocumentFieldId);
        Assert.True(version.CheckRequiredFields(hidden).IsSuccess);

        var shown = version.BuildValues(id, new[] { In("kind", "other"), In("reason", "سبب") }).Value;
        Assert.Equal(2, shown.Count);

        var missing = version.BuildValues(id, new[] { In("kind", "other") }).Value;
        Assert.Equal("DocumentFields.ValueRequired", version.CheckRequiredFields(missing).Error.Code);
    }

    [Fact]
    public void Visibility_FollowsAChainAndMultiSelectChoices()
    {
        var kinds = Field("kinds", DocumentFieldDataType.MultiSelect);
        var flag = Field("flag", DocumentFieldDataType.Boolean);
        var note = Field("note", DocumentFieldDataType.Text);
        var version = Draft(
            (kinds, Settings(DocumentFieldDataType.MultiSelect, options: Kinds)),
            (flag, Settings(DocumentFieldDataType.Boolean,
                new DocumentFieldBehavior(VisibleWhenFieldKey: "kinds", VisibleWhenValue: "loan"))),
            (note, Settings(DocumentFieldDataType.Text,
                new DocumentFieldBehavior(VisibleWhenFieldKey: "flag", VisibleWhenValue: "true"))));
        var id = Guid.NewGuid();

        var open = version.BuildValues(id, new[] { In("kinds", "loan,other"), In("flag", "true"), In("note", "x") });
        Assert.Equal(3, open.Value.Count);

        // The flag still says true, but it is hidden now, so the note hangs on a hidden field.
        var closed = version.BuildValues(id, new[] { In("kinds", "other"), In("flag", "true"), In("note", "x") });
        Assert.Equal(kinds.DocumentFieldId, Assert.Single(closed.Value).DocumentFieldId);
    }

    [Fact]
    public void Conditions_AreCheckedWhenConfigured()
    {
        var kind = Field("kind", DocumentFieldDataType.Select);
        var text = Field("text", DocumentFieldDataType.Text);
        var version = Draft((kind, Settings(DocumentFieldDataType.Select, options: Kinds)),
            (text, Settings(DocumentFieldDataType.Text)));

        Assert.Equal("DocumentFields.ConditionFieldInvalid", version.UpdateField(text.DocumentFieldId,
            Settings(DocumentFieldDataType.Text, new DocumentFieldBehavior(VisibleWhenFieldKey: "missing", VisibleWhenValue: "x"))).Error.Code);
        Assert.Equal("DocumentFields.ConditionFieldInvalid", version.UpdateField(kind.DocumentFieldId,
            Settings(DocumentFieldDataType.Select, new DocumentFieldBehavior(VisibleWhenFieldKey: "text", VisibleWhenValue: "x"), options: Kinds)).Error.Code);
        Assert.Equal("DocumentFields.ConditionValueInvalid", version.UpdateField(text.DocumentFieldId,
            Settings(DocumentFieldDataType.Text, new DocumentFieldBehavior(VisibleWhenFieldKey: "kind", VisibleWhenValue: "nope"))).Error.Code);

        Assert.True(version.UpdateField(text.DocumentFieldId,
            Settings(DocumentFieldDataType.Text, new DocumentFieldBehavior(VisibleWhenFieldKey: "kind", VisibleWhenValue: "loan"))).IsSuccess);
        Assert.Equal("DocumentFields.FieldHasDependents", version.RemoveField(kind.DocumentFieldId).Error.Code);

        // Removing the option that a condition points at is refused too.
        Assert.Equal("DocumentFields.ConditionValueInvalid", version.UpdateField(kind.DocumentFieldId,
            Settings(DocumentFieldDataType.Select, options: new[] { new DocumentFieldOption("other", "أخرى") })).Error.Code);
    }

    [Fact]
    public void Conditions_CannotFormACircle()
    {
        var a = Field("aa", DocumentFieldDataType.Boolean);
        var b = Field("bb", DocumentFieldDataType.Boolean);
        var version = Draft((a, Settings(DocumentFieldDataType.Boolean)),
            (b, Settings(DocumentFieldDataType.Boolean, new DocumentFieldBehavior(VisibleWhenFieldKey: "aa", VisibleWhenValue: "true"))));

        Assert.Equal("DocumentFields.ConditionCycle", version.UpdateField(a.DocumentFieldId,
            Settings(DocumentFieldDataType.Boolean, new DocumentFieldBehavior(VisibleWhenFieldKey: "bb", VisibleWhenValue: "true"))).Error.Code);
    }

    [Fact]
    public void Limits_MinLengthAndDates()
    {
        var text = Field("text", DocumentFieldDataType.Text);
        var date = Field("date", DocumentFieldDataType.Date);
        var version = Draft(
            (text, Settings(DocumentFieldDataType.Text, new DocumentFieldBehavior(MinLength: 3))),
            (date, Settings(DocumentFieldDataType.Date,
                new DocumentFieldBehavior(MinDate: "2026-01-01", MaxDate: DocumentFieldBehavior.TodayToken))));
        var id = Guid.NewGuid();

        Assert.Equal("DocumentFields.ValueTooShort", version.BuildValues(id, new[] { In("text", "ab") }, utcNow: Now).Error.Code);
        Assert.Equal("DocumentFields.ValueDateOutOfRange", version.BuildValues(id, new[] { In("date", "2025-12-31") }, utcNow: Now).Error.Code);
        Assert.Equal("DocumentFields.ValueDateOutOfRange", version.BuildValues(id, new[] { In("date", "2026-11-12") }, utcNow: Now).Error.Code);
        // A local "today" can be a calendar day ahead of UTC.
        Assert.True(version.BuildValues(id, new[] { In("date", "2026-11-09") }, utcNow: Now).IsSuccess);

        Assert.Equal("DocumentFields.DateRangeInvalid", ErrorOf(DocumentFieldDataType.Date,
            new DocumentFieldBehavior(MinDate: "2026-02-01", MaxDate: "2026-01-01")).Code);
        Assert.Equal("DocumentFields.DateRangeNotAllowed", ErrorOf(DocumentFieldDataType.Text,
            new DocumentFieldBehavior(MinDate: "2026-02-01")).Code);
        Assert.Equal("DocumentFields.MinLengthNotAllowed", ErrorOf(DocumentFieldDataType.Number,
            new DocumentFieldBehavior(MinLength: 1)).Code);
    }

    [Fact]
    public void MultiSelect_StoresEveryChoiceOnce_AndRejectsUnknownOnes()
    {
        var kinds = Field("kinds", DocumentFieldDataType.MultiSelect);
        var version = Draft((kinds, Settings(DocumentFieldDataType.MultiSelect, options: Kinds)));
        var id = Guid.NewGuid();

        Assert.Equal("|loan|other|", Assert.Single(version.BuildValues(id, new[] { In("kinds", "loan, other,loan") }).Value).TextValue);
        Assert.Equal("DocumentFields.ValueOptionInvalid", version.BuildValues(id, new[] { In("kinds", "loan,nope") }).Error.Code);
    }

    [Fact]
    public void OptionSource_ValidatesAgainstTheResolvedChoices_AndKeepsNoListOfItsOwn()
    {
        var department = Field("dept", DocumentFieldDataType.Select);
        var source = new DocumentFieldBehavior(OptionSource: DocumentFieldOptionSource.Departments);
        var version = Draft((department, Settings(DocumentFieldDataType.Select, source)));
        var id = Guid.NewGuid();
        var resolved = new Dictionary<Guid, IReadOnlyList<DocumentFieldOption>>
        {
            [department.DocumentFieldId] = new[] { new DocumentFieldOption("d-1", "دائرة") }
        };

        Assert.Equal("d-1", Assert.Single(version.BuildValues(id, new[] { In("dept", "d-1") }, sourcedOptions: resolved).Value).OptionKey);
        Assert.Equal("DocumentFields.ValueOptionInvalid", version.BuildValues(id, new[] { In("dept", "d-2") }, sourcedOptions: resolved).Error.Code);

        Assert.Equal("DocumentFields.OptionsNotAllowed", ErrorOf(DocumentFieldDataType.Select, source, Kinds).Code);
        Assert.Equal("DocumentFields.OptionSourceInvalid", ErrorOf(DocumentFieldDataType.Select,
            new DocumentFieldBehavior(OptionSource: DocumentFieldOptionSource.Lookup)).Code);
        Assert.Equal("DocumentFields.OptionSourceNotAllowed", ErrorOf(DocumentFieldDataType.Text, source).Code);
        Assert.Equal("DocumentFields.DefaultValueInvalid", ErrorOf(DocumentFieldDataType.Select,
            source with { DefaultValue = "d-1" }).Code);
    }

    [Fact]
    public void Draft_CopiesBehaviorFromThePublishedVersion()
    {
        var code = Field("code", DocumentFieldDataType.Text);
        var version = Draft((code, Settings(DocumentFieldDataType.Text,
            new DocumentFieldBehavior(PlaceholderAr: "مثال", IsHalfWidth: true, ShowInTable: true))));
        version.Publish(Actor);

        var next = DocumentTypeSchemaVersion.CreateDraftFrom(version, 2, Actor).Value;

        Assert.Equal(new DocumentFieldBehavior(PlaceholderAr: "مثال", IsHalfWidth: true, ShowInTable: true),
            Assert.Single(next.Fields).Behavior);
    }
}
