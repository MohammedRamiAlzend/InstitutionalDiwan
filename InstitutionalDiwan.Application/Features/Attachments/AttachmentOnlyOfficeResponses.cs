namespace InstitutionalDiwan.Application.Features.Attachments;

public sealed record AttachmentEditConfigResponse(
    string ServerUrl,
    string DocumentType,
    string Mode,
    int VersionNumber,
    IReadOnlyDictionary<string, object?> Config);

public sealed record AttachmentOnlyOfficeCallbackResult(int Error)
{
    public static AttachmentOnlyOfficeCallbackResult Ok { get; } = new(0);
    public static AttachmentOnlyOfficeCallbackResult Failed { get; } = new(1);
}

public sealed record AttachmentOnlyOfficeCallbackPayload(
    string? Key,
    int Status,
    string? Url,
    string? Token);

internal static class AttachmentDocumentTypes
{
    private static readonly HashSet<string> Word = new(StringComparer.OrdinalIgnoreCase)
    {
        "doc", "docx", "docm", "dot", "dotx", "dotm", "odt", "ott", "rtf", "txt"
    };

    private static readonly HashSet<string> Cell = new(StringComparer.OrdinalIgnoreCase)
    {
        "xls", "xlsx", "xlsm", "xlt", "xltx", "xltm", "ods", "ots", "csv"
    };

    public static string? DocumentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.');
        if (Word.Contains(extension)) return "word";
        if (Cell.Contains(extension)) return "cell";
        return null;
    }

    public static bool IsEditable(string fileName) =>
        DocumentType(fileName) is not null;

    public static string FileType(string fileName) =>
        Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
}
