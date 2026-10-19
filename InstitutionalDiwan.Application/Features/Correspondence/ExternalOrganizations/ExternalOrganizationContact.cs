using System.Net.Mail;
using System.Text.RegularExpressions;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

/// <summary>
/// Contact information on an external organization is a single free-text field that must hold
/// either an e-mail address or a phone number (optionally with +, spaces, dashes, or parentheses).
/// </summary>
internal static partial class ExternalOrganizationContact
{
    public static bool IsValid(string? contactInfo)
    {
        if (string.IsNullOrWhiteSpace(contactInfo))
            return true;

        var value = contactInfo.Trim();
        if (value.Contains('@', StringComparison.Ordinal))
        {
            return value.Length <= 254 &&
                MailAddress.TryCreate(value, out var address) &&
                string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase);
        }

        return PhonePattern().IsMatch(value);
    }

    [GeneratedRegex(@"^\+?[0-9][0-9\s\-()]{5,19}$")]
    private static partial Regex PhonePattern();
}
