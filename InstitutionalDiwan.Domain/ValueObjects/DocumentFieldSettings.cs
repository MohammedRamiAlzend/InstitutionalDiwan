using System.Text.RegularExpressions;

namespace InstitutionalDiwan.Domain.ValueObjects;

/// <summary>One choice of a <see cref="DocumentFieldDataType.Select"/> field.</summary>
public sealed record DocumentFieldOption(string Key, string LabelAr);

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
        IReadOnlyList<DocumentFieldOption> options)
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
        IReadOnlyList<DocumentFieldOption>? options = null)
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

        var optionList = options ?? Array.Empty<DocumentFieldOption>();
        if (dataType == DocumentFieldDataType.Select)
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

        return Result.Success(new DocumentFieldSettings(
            dataType, label, isRequired, displayOrder, pattern, patternMessage, minNumber, maxNumber, maxLength, optionList));
    }
}
