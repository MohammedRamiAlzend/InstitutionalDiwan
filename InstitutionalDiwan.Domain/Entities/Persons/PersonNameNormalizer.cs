using System.Text;

namespace InstitutionalDiwan.Domain.Entities.Persons;

/// <summary>
/// Folds a name to the form it is stored and searched in, so that common Arabic spelling variants
/// (hamza forms, taa marbuta, alef maqsura, diacritics) match each other.
/// </summary>
public static class PersonNameNormalizer
{
    /// <summary>The folded text with single spaces, or <c>null</c> when nothing is left of it.</summary>
    public static string? Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var folded = new StringBuilder(text.Length);
        var pendingSpace = false;

        foreach (var character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = folded.Length > 0;
                continue;
            }

            if (IsIgnorable(character))
                continue;

            if (pendingSpace)
            {
                folded.Append(' ');
                pendingSpace = false;
            }

            folded.Append(Fold(character));
        }

        return folded.Length == 0 ? null : folded.ToString();
    }

    /// <summary>The distinct words of the folded text, for matching each one on its own.</summary>
    public static IReadOnlyList<string> Tokens(string? text) =>
        Normalize(text)?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).ToList()
        ?? [];

    // Harakat, superscript alef, and the tatweel carry no identity in a name.
    private static bool IsIgnorable(char character) =>
        character is >= 'ً' and <= 'ٟ' or 'ٰ' or 'ـ';

    private static char Fold(char character) => character switch
    {
        'أ' or 'إ' or 'آ' or 'ٱ' => 'ا', // أ إ آ ٱ → ا
        'ة' => 'ه', // ة → ه
        'ى' => 'ي', // ى → ي
        'ؤ' => 'و', // ؤ → و
        'ئ' => 'ي', // ئ → ي
        _ => char.ToLowerInvariant(character),
    };
}
