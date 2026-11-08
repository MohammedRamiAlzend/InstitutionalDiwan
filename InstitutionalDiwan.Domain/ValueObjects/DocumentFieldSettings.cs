using System.Globalization;
using System.Text.RegularExpressions;
using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Domain.ValueObjects;

/// <summary>
/// One choice of a select or multi-select field. <paramref name="IsActive"/> is false only for a choice
/// resolved from a source whose entry was deactivated: it stays readable but is no longer offered.
/// </summary>
public sealed record DocumentFieldOption(string Key, string LabelAr, bool IsActive = true);

/// <summary>
/// How a dynamic field behaves inside one schema version: label, requiredness, order and the
/// type-specific constraints (pattern for text, range for numbers, options for selects). Validated
/// once at construction so a schema version can never hold an unusable field.
/// </summary>
public sealed record DocumentFieldSettings
{
    public const int MaxLabelLength = 200;
    public const int MaxRegexLength = 500;
    public const int MaxRegexMessageLength = 500;
    /// <summary>Numbers (values and range limits) must stay below this magnitude so they fit the stored decimal(28,8) column.</summary>
    public const decimal MaxAbsoluteNumber = 1e20m;
    public const int MaxTextLength = 500;
    public const int MaxLongTextLength = 4000;
    public const int MaxOptions = 100;

    /// <summary>Upper bound for evaluating one field pattern; guards against catastrophic backtracking.</summary>
    public static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    public DocumentFieldDataType DataType { get; }
    public string LabelAr { get; }
    public bool IsRequired { get; }
    public int DisplayOrder { get; }
    public string? RegexPattern { get; }
    public string? RegexErrorMessageAr { get; }
    public decimal? MinNumber { get; }
    public decimal? MaxNumber { get; }
    public int? MaxLength { get; }
    public IReadOnlyList<DocumentFieldOption> Options { get; }
    public DocumentFieldBehavior Behavior { get; }

    private DocumentFieldSettings(
        DocumentFieldDataType dataType,
        string labelAr,
        bool isRequired,
        int displayOrder,
        string? regexPattern,
        string? regexErrorMessageAr,
        decimal? minNumber,
        decimal? maxNumber,
        int? maxLength,
        IReadOnlyList<DocumentFieldOption> options,
        DocumentFieldBehavior behavior)
    {
        DataType = dataType;
        LabelAr = labelAr;
        IsRequired = isRequired;
        DisplayOrder = displayOrder;
        RegexPattern = regexPattern;
        RegexErrorMessageAr = regexErrorMessageAr;
        MinNumber = minNumber;
        MaxNumber = maxNumber;
        MaxLength = maxLength;
        Options = options;
        Behavior = behavior;
    }

    public static Result<DocumentFieldSettings> Create(
        DocumentFieldDataType dataType,
        string? labelAr,
        bool isRequired,
        int displayOrder,
        string? regexPattern = null,
        string? regexErrorMessageAr = null,
        decimal? minNumber = null,
        decimal? maxNumber = null,
        int? maxLength = null,
        IReadOnlyList<DocumentFieldOption>? options = null,
        DocumentFieldBehavior? behavior = null)
    {
        if (!Enum.IsDefined(dataType))
            return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.DataTypeInvalid);

        var label = labelAr?.Trim();
        if (string.IsNullOrEmpty(label))
            return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.LabelEmpty);
        if (label.Length > MaxLabelLength)
            return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.LabelTooLong);

        var isText = dataType is DocumentFieldDataType.Text or DocumentFieldDataType.LongText;

        var pattern = string.IsNullOrWhiteSpace(regexPattern) ? null : regexPattern;
        var patternMessage = string.IsNullOrWhiteSpace(regexErrorMessageAr) ? null : regexErrorMessageAr.Trim();
        if (pattern is not null)
        {
            if (!isText)
                return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.RegexNotAllowed);
            if (pattern.Length > MaxRegexLength)
                return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.RegexTooLong);
            if (patternMessage is null)
                return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.RegexMessageRequired);
            if (patternMessage.Length > MaxRegexMessageLength)
                return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.RegexMessageTooLong);
            try
            {
                _ = new Regex(pattern, RegexOptions.CultureInvariant, RegexTimeout);
            }
            catch (ArgumentException)
            {
                return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.RegexInvalid);
            }
        }
        else
        {
            patternMessage = null;
        }

        if (maxLength is not null)
        {
            if (!isText)
                return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.MaxLengthNotAllowed);
            var limit = dataType == DocumentFieldDataType.Text ? MaxTextLength : MaxLongTextLength;
            if (maxLength < 1 || maxLength > limit)
                return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.MaxLengthInvalid);
        }

        if (minNumber is not null || maxNumber is not null)
        {
            if (dataType != DocumentFieldDataType.Number)
                return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.NumberRangeNotAllowed);
            if (minNumber is { } min && maxNumber is { } max && min > max)
                return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.NumberRangeInvalid);
            if (Math.Abs(minNumber ?? 0m) >= MaxAbsoluteNumber || Math.Abs(maxNumber ?? 0m) >= MaxAbsoluteNumber)
                return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.NumberRangeInvalid);
        }

        var requestedBehavior = behavior ?? new DocumentFieldBehavior();
        var isChoice = dataType is DocumentFieldDataType.Select or DocumentFieldDataType.MultiSelect;
        if (requestedBehavior.OptionSource != DocumentFieldOptionSource.Static && !isChoice)
            return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.OptionSourceNotAllowed);

        var optionList = options ?? Array.Empty<DocumentFieldOption>();
        if (isChoice && requestedBehavior.OptionSource != DocumentFieldOptionSource.Static)
        {
            // The choices are read from the source on demand, so the field keeps no list of its own.
            if (optionList.Count > 0)
                return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.OptionsNotAllowed);
        }
        else if (isChoice)
        {
            if (optionList.Count == 0)
                return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.OptionsRequired);
            if (optionList.Count > MaxOptions)
                return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.TooManyOptions);

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var normalized = new List<DocumentFieldOption>(optionList.Count);
            foreach (var option in optionList)
            {
                var key = option.Key?.Trim();
                var optionLabel = option.LabelAr?.Trim();
                if (!DocumentField.IsValidKey(key) || string.IsNullOrEmpty(optionLabel) || optionLabel.Length > MaxLabelLength)
                    return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.OptionInvalid);
                if (!seen.Add(key!))
                    return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.OptionKeyDuplicate);
                normalized.Add(new DocumentFieldOption(key!, optionLabel));
            }

            optionList = normalized;
        }
        else if (optionList.Count > 0)
        {
            return Result.Failure<DocumentFieldSettings>(DomainErrors.DocumentFields.OptionsNotAllowed);
        }

        var normalizedBehavior = NormalizeBehavior(dataType, requestedBehavior, maxLength);
        if (normalizedBehavior.IsFailure)
            return Result.Failure<DocumentFieldSettings>(normalizedBehavior.Error);

        var settings = new DocumentFieldSettings(
            dataType, label, isRequired, displayOrder, pattern, patternMessage, minNumber, maxNumber, maxLength,
            optionList, normalizedBehavior.Value);

        var defaultCheck = DocumentTypeSchemaVersionField.ValidateDefault(settings);
        return defaultCheck.IsFailure
            ? Result.Failure<DocumentFieldSettings>(defaultCheck.Error)
            : Result.Success(settings);
    }

    /// <summary>Checks the options that do not depend on the other fields, and blanks empty text.</summary>
    private static Result<DocumentFieldBehavior> NormalizeBehavior(
        DocumentFieldDataType dataType,
        DocumentFieldBehavior behavior,
        int? maxLength)
    {
        var placeholder = Clean(behavior.PlaceholderAr);
        var help = Clean(behavior.HelpTextAr);
        var section = Clean(behavior.SectionTitleAr);
        if (placeholder?.Length > DocumentFieldBehavior.MaxPlaceholderLength
            || help?.Length > DocumentFieldBehavior.MaxHelpTextLength
            || section?.Length > DocumentFieldBehavior.MaxSectionTitleLength)
        {
            return Result.Failure<DocumentFieldBehavior>(DomainErrors.DocumentFields.BehaviorTextTooLong);
        }

        if (behavior.MinLength is { } minLength)
        {
            if (dataType is not (DocumentFieldDataType.Text or DocumentFieldDataType.LongText))
                return Result.Failure<DocumentFieldBehavior>(DomainErrors.DocumentFields.MinLengthNotAllowed);
            var limit = maxLength ?? (dataType == DocumentFieldDataType.Text ? MaxTextLength : MaxLongTextLength);
            if (minLength < 1 || minLength > limit)
                return Result.Failure<DocumentFieldBehavior>(DomainErrors.DocumentFields.MinLengthInvalid);
        }

        var minDate = Clean(behavior.MinDate);
        var maxDate = Clean(behavior.MaxDate);
        if (minDate is not null || maxDate is not null)
        {
            if (dataType != DocumentFieldDataType.Date)
                return Result.Failure<DocumentFieldBehavior>(DomainErrors.DocumentFields.DateRangeNotAllowed);
            if (!IsDateOrToday(minDate, out var from) || !IsDateOrToday(maxDate, out var to))
                return Result.Failure<DocumentFieldBehavior>(DomainErrors.DocumentFields.DateRangeInvalid);
            if (from is not null && to is not null && from > to)
                return Result.Failure<DocumentFieldBehavior>(DomainErrors.DocumentFields.DateRangeInvalid);
        }

        var conditionKey = Clean(behavior.VisibleWhenFieldKey);
        var conditionValue = Clean(behavior.VisibleWhenValue);
        if ((conditionKey is null) != (conditionValue is null))
            return Result.Failure<DocumentFieldBehavior>(DomainErrors.DocumentFields.ConditionIncomplete);

        var catalog = Clean(behavior.LookupCatalog);
        if (behavior.OptionSource == DocumentFieldOptionSource.Lookup)
        {
            if (catalog is null || catalog.Length > DocumentFieldBehavior.MaxLookupCatalogLength)
                return Result.Failure<DocumentFieldBehavior>(DomainErrors.DocumentFields.OptionSourceInvalid);
        }
        else
        {
            catalog = null;
        }

        var defaultValue = Clean(behavior.DefaultValue);
        if (defaultValue?.Length > DocumentFieldBehavior.MaxDefaultValueLength)
            return Result.Failure<DocumentFieldBehavior>(DomainErrors.DocumentFields.DefaultValueInvalid);
        // A source's entries are unknown here and may disappear, so only typed lists can have a default.
        if (defaultValue is not null && behavior.OptionSource != DocumentFieldOptionSource.Static)
            return Result.Failure<DocumentFieldBehavior>(DomainErrors.DocumentFields.DefaultValueInvalid);
        if (behavior.IsReadOnly && defaultValue is null)
            return Result.Failure<DocumentFieldBehavior>(DomainErrors.DocumentFields.ReadOnlyRequiresDefault);

        return Result.Success(behavior with
        {
            DefaultValue = defaultValue,
            PlaceholderAr = placeholder,
            HelpTextAr = help,
            SectionTitleAr = section,
            MinDate = minDate,
            MaxDate = maxDate,
            VisibleWhenFieldKey = conditionKey,
            VisibleWhenValue = conditionValue,
            LookupCatalog = catalog
        });
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    /// <summary>False for unreadable text; <paramref name="date"/> stays null for no bound or the moving "today".</summary>
    private static bool IsDateOrToday(string? text, out DateTime? date)
    {
        date = null;
        if (text is null || text == DocumentFieldBehavior.TodayToken)
            return true;
        if (!DateTime.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
            return false;

        date = parsed;
        return true;
    }
}
