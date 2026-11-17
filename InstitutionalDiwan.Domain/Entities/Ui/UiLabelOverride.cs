using System.Text.RegularExpressions;
using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Domain.Entities.Ui;

/// <summary>
/// A superadmin replacement for one built-in UI label (button, page title, table column, sidebar
/// entry). Only overridden keys are stored; an absent key means the front end's built-in default
/// applies. Keys are lowercase dot-separated identifiers such as <c>nav.documents</c>.
/// </summary>
public sealed class UiLabelOverride : AggregateRoot<Guid>
{
    public const int MaxKeyLength = 150;
    public const int MaxValueLength = 200;

    private static readonly Regex KeyPattern = new("^[a-z0-9]+([.\\-][a-z0-9]+)*$", RegexOptions.Compiled);

    public Guid LabelId
    {
        get => Id;
        private set => Id = value;
    }

    public string Key { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public DateTime UpdatedAt { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    private UiLabelOverride()
    {
    }

    public static Result<UiLabelOverride> Create(
        string? key,
        string? value,
        Guid? updatedByUserId = null,
        DateTime? utcNow = null)
    {
        var normalizedKey = NormalizeKey(key);
        if (normalizedKey is null || !KeyPattern.IsMatch(normalizedKey))
            return Result.Failure<UiLabelOverride>(DomainErrors.UiLabel.KeyInvalid);

        var normalizedValue = NormalizeValue(value);
        if (normalizedValue is null)
            return Result.Failure<UiLabelOverride>(DomainErrors.UiLabel.ValueEmpty);
        if (normalizedValue.Length > MaxValueLength)
            return Result.Failure<UiLabelOverride>(DomainErrors.UiLabel.ValueTooLong);

        return new UiLabelOverride
        {
            LabelId = Guid.NewGuid(),
            Key = normalizedKey,
            Value = normalizedValue,
            UpdatedAt = utcNow ?? DateTime.UtcNow,
            UpdatedByUserId = updatedByUserId
        };
    }

    public Result UpdateValue(string? value, Guid? updatedByUserId = null, DateTime? utcNow = null)
    {
        var normalizedValue = NormalizeValue(value);
        if (normalizedValue is null)
            return Result.Failure(DomainErrors.UiLabel.ValueEmpty);
        if (normalizedValue.Length > MaxValueLength)
            return Result.Failure(DomainErrors.UiLabel.ValueTooLong);

        Value = normalizedValue;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
        return Result.Success();
    }

    /// <summary>Trims and lowercases a key; null when empty or longer than <see cref="MaxKeyLength"/>.</summary>
    public static string? NormalizeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var normalized = key.Trim().ToLowerInvariant();
        return normalized.Length > MaxKeyLength ? null : normalized;
    }

    private static string? NormalizeValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}