using InstitutionalDiwan.Domain.Entities.Drive;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>
/// The OnlyOffice editor configuration for a Drive file (Module 11). <paramref name="Config"/> is the
/// full config object (document / documentType / editorConfig / token) the browser passes to
/// <c>DocsAPI.DocEditor</c>; <paramref name="ServerUrl"/> is where the browser loads the editor API.
/// </summary>
public sealed record DriveEditConfigResponse(
    string ServerUrl,
    string DocumentType,
    string Mode,
    Guid SessionId,
    int VersionNumber,
    IReadOnlyDictionary<string, object?> Config);

/// <summary>The OnlyOffice callback protocol answer: <c>error=0</c> acknowledges, any other value reports a failure.</summary>
public sealed record DriveOnlyOfficeCallbackResult(int Error)
{
    public static DriveOnlyOfficeCallbackResult Ok { get; } = new(0);
    public static DriveOnlyOfficeCallbackResult Failed { get; } = new(1);
}

/// <summary>
/// The save-callback body OnlyOffice posts (Module 11). <paramref name="Token"/> is the HS256 JWT
/// OnlyOffice signs with the shared secret; <paramref name="Key"/> identifies the edit session.
/// </summary>
public sealed record DriveOnlyOfficeCallbackPayload(
    string? Key,
    int Status,
    string? Url,
    IReadOnlyList<string>? Users,
    int? ForceSaveType,
    string? Token);

/// <summary>Maps a file extension to the OnlyOffice document type and whether it is editable.</summary>
internal static class DriveDocumentTypes
{
    private static readonly HashSet<string> WordExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "doc", "docx", "docm", "dot", "dotx", "dotm", "odt", "ott", "rtf", "txt",
        "html", "htm", "mht", "pdf", "djvu", "fb2", "epub", "xps"
    };

    private static readonly HashSet<string> CellExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "xls", "xlsx", "xlsm", "xlt", "xltx", "xltm", "ods", "ots", "csv"
    };

    private static readonly HashSet<string> SlideExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "ppt", "pptx", "pptm", "pot", "potx", "potm", "pps", "ppsx", "ppsm", "odp", "otp"
    };

    private static readonly HashSet<string> EditableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "doc", "docx", "odt", "rtf", "txt", "xls", "xlsx", "ods", "csv", "ppt", "pptx", "odp"
    };

    /// <summary>Returns the OnlyOffice document type (word/cell/slide), or null when unsupported.</summary>
    public static string? DocumentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.');
        if (WordExtensions.Contains(extension))
            return "word";
        if (CellExtensions.Contains(extension))
            return "cell";
        if (SlideExtensions.Contains(extension))
            return "slide";
        return null;
    }

    public static bool IsEditable(string fileName) =>
        EditableExtensions.Contains(Path.GetExtension(fileName).TrimStart('.'));

    public static string FileType(string fileName) =>
        Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
}