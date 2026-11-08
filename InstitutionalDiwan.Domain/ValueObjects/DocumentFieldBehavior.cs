namespace InstitutionalDiwan.Domain.ValueObjects;

/// <summary>Where the choices of a select or multi-select field come from. Stored as its integer value.</summary>
public enum DocumentFieldOptionSource
{
    /// <summary>The list typed into the field's own settings.</summary>
    Static = 0,
    Departments = 1,
    ExternalOrganizations = 2,
    /// <summary>A lookup catalog named by <see cref="DocumentFieldBehavior.LookupCatalog"/>.</summary>
    Lookup = 3
}

/// <summary>
/// The form and registry behavior of a field inside one schema version. Kept as one always-read-together
/// JSON value (like the option list), so a new option needs no column and a published version stays
/// frozen. Validated by <see cref="DocumentFieldSettings.Create"/>.
/// </summary>
/// <param name="DefaultValue">Text form of a value, or <see cref="TodayToken"/> on a date field.</param>
/// <param name="IsReadOnly">The server writes <paramref name="DefaultValue"/> itself and refuses any other value.</param>
/// <param name="IsFilterable">Offered as a filter on the document tables.</param>
/// <param name="ShowInTable">Offered as a column of the document registry.</param>
/// <param name="MinDate">A date in ISO form, or <see cref="TodayToken"/>.</param>
/// <param name="MaxDate">A date in ISO form, or <see cref="TodayToken"/>.</param>
/// <param name="VisibleWhenFieldKey">The boolean, select or multi-select field this one depends on.</param>
/// <param name="VisibleWhenValue">The value of that field that shows this one; a multi-select needs it among its choices.</param>
/// <param name="IsHalfWidth">Takes half a form row instead of a full one.</param>
/// <param name="SectionTitleAr">A heading printed before this field when it differs from the previous field's.</param>
public sealed record DocumentFieldBehavior(
    string? DefaultValue = null,
    bool IsReadOnly = false,
    bool IsFilterable = false,
    bool ShowInTable = false,
    string? PlaceholderAr = null,
    string? HelpTextAr = null,
    int? MinLength = null,
    string? MinDate = null,
    string? MaxDate = null,
    string? VisibleWhenFieldKey = null,
    string? VisibleWhenValue = null,
    bool IsHalfWidth = false,
    string? SectionTitleAr = null,
    DocumentFieldOptionSource OptionSource = DocumentFieldOptionSource.Static,
    string? LookupCatalog = null)
{
    /// <summary>Stands for the current date in <see cref="DefaultValue"/>, <see cref="MinDate"/> and <see cref="MaxDate"/>.</summary>
    public const string TodayToken = "@today";

    public const int MaxDefaultValueLength = DocumentFieldSettings.MaxLongTextLength;
    public const int MaxPlaceholderLength = 200;
    public const int MaxHelpTextLength = 500;
    public const int MaxSectionTitleLength = 100;
    public const int MaxLookupCatalogLength = 100;
    /// <summary>Most choices one multi-select value may hold, so its stored text always fits the column.</summary>
    public const int MaxSelections = 50;
}
