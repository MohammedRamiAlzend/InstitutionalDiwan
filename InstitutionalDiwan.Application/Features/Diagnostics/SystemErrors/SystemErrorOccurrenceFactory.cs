using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using InstitutionalDiwan.Domain.Entities.Diagnostics;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Diagnostics.SystemErrors;

/// <summary>Who and what surrounded a failure. Query strings and request bodies are deliberately absent.</summary>
public sealed record SystemErrorContext(
    string? Path,
    string? HttpMethod,
    Guid? UserId,
    string? Username,
    string? UserAgent,
    string? TraceId,
    DateTime OccurredAt);

/// <summary>
/// Turns a failure into an occurrence: bounded text, plus a fingerprint that stays the same when the
/// same defect repeats, so ids and numbers in a message or path do not split it into many rows.
/// </summary>
public static class SystemErrorOccurrenceFactory
{
    private const string OwnCodeMarker = "InstitutionalDiwan.";

    private static readonly Regex Identifiers = new(
        @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b|\d+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static SystemErrorOccurrence ForException(Exception exception, int statusCode, SystemErrorContext context)
    {
        var details = exception.ToString();
        var title = exception.GetType().FullName ?? exception.GetType().Name;
        return Build(
            SystemErrorSource.Server, SystemErrorKind.ServerException, title, exception.Message, details, statusCode,
            context, title, FirstOwnStackFrame(details));
    }

    public static SystemErrorOccurrence ForRejection(string title, string? detail, int statusCode, SystemErrorContext context)
    {
        var kind = statusCode >= 500 ? SystemErrorKind.ServerException : SystemErrorKind.ServerRejection;
        return Build(
            SystemErrorSource.Server, kind, title, detail ?? title, null, statusCode,
            context, title, statusCode.ToString(), context.HttpMethod, Normalize(context.Path));
    }

    public static SystemErrorOccurrence ForClient(
        SystemErrorKind kind,
        string message,
        string? details,
        int? statusCode,
        SystemErrorContext context)
    {
        var firstLine = message.Split('\n', 2)[0];
        // A network failure is the same defect per endpoint; a script failure is the same per code location.
        string[] identity = kind == SystemErrorKind.ClientNetwork
            ? [context.HttpMethod ?? string.Empty, Normalize(context.Path), statusCode?.ToString() ?? string.Empty]
            : [Normalize(firstLine), FirstClientStackFrame(details)];

        return Build(
            SystemErrorSource.Client, kind, Limit(firstLine, 300)!, message, details, statusCode,
            context, identity);
    }

    private static SystemErrorOccurrence Build(
        SystemErrorSource source,
        SystemErrorKind kind,
        string title,
        string message,
        string? details,
        int? statusCode,
        SystemErrorContext context,
        params string?[] identity) =>
        new(
            Hash(source, kind, identity),
            source,
            kind,
            Limit(title, SystemError.MaxTitleLength)!,
            Limit(message, SystemError.MaxMessageLength)!,
            Limit(details, SystemError.MaxDetailsLength),
            Limit(context.Path, SystemError.MaxPathLength),
            Limit(context.HttpMethod, 10),
            statusCode,
            context.UserId,
            Limit(context.Username, 200),
            Limit(context.UserAgent, 500),
            Limit(context.TraceId, 100),
            context.OccurredAt);

    private static string Hash(SystemErrorSource source, SystemErrorKind kind, IEnumerable<string?> identity)
    {
        var text = $"{(int)source}|{(int)kind}|{string.Join('|', identity)}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }

    private static string Normalize(string? text) =>
        text is null ? string.Empty : Identifiers.Replace(text, "#");

    /// <summary>The first frame in this codebase says where the defect is; framework frames do not.</summary>
    private static string FirstOwnStackFrame(string details)
    {
        var frames = details.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("at ", StringComparison.Ordinal))
            .ToList();
        var frame = frames.FirstOrDefault(line => line.Contains(OwnCodeMarker, StringComparison.Ordinal))
            ?? frames.FirstOrDefault()
            ?? string.Empty;
        // A rebuild shifts line numbers; " in ...:line N" must not turn one defect into a new row.
        var location = frame.IndexOf(" in ", StringComparison.Ordinal);
        return location < 0 ? frame : frame[..location];
    }

    /// <summary>Drops query strings and line numbers, which change with every build of the page.</summary>
    private static string FirstClientStackFrame(string? details)
    {
        var frame = details?.Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("at ", StringComparison.Ordinal))
            ?? string.Empty;
        var name = frame.IndexOf(" (", StringComparison.Ordinal);
        return name < 0 ? Normalize(frame) : frame[..name];
    }

    private static string? Limit(string? text, int length) =>
        text is null || text.Length <= length ? text : text[..length];
}
