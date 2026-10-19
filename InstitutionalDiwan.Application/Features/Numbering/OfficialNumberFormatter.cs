using System.Globalization;
using System.Text.RegularExpressions;

namespace InstitutionalDiwan.Application.Features.Numbering;

/// <summary>
/// Renders an official number from a registry template. Supported tokens are <c>{YEAR}</c> and
/// <c>{SEQ}</c> with an optional standard numeric format specifier (<c>{SEQ:D6}</c>). Unknown text is
/// preserved verbatim, so a registry may use any separators or prefixes it needs.
/// </summary>
internal static partial class OfficialNumberFormatter
{
    public const string DefaultPattern = "{YEAR}/{SEQ:D6}";

    public static string Format(string? pattern, int numberingYear, long sequenceValue)
    {
        var template = string.IsNullOrWhiteSpace(pattern) ? DefaultPattern : pattern.Trim();

        var withYear = template.Replace(
            "{YEAR}",
            numberingYear.ToString(CultureInfo.InvariantCulture),
            StringComparison.OrdinalIgnoreCase);

        return SequenceToken().Replace(withYear, match =>
        {
            var digits = match.Groups["digits"].Success
                ? int.Parse(match.Groups["digits"].Value, CultureInfo.InvariantCulture)
                : 0;

            return digits > 0
                ? sequenceValue.ToString("D" + digits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
                : sequenceValue.ToString(CultureInfo.InvariantCulture);
        });
    }

    [GeneratedRegex(@"\{SEQ(?::D(?<digits>\d{1,3}))?\}", RegexOptions.IgnoreCase)]
    private static partial Regex SequenceToken();
}
