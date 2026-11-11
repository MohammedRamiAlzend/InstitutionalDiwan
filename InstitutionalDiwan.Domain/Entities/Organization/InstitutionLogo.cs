using System.Text;

namespace InstitutionalDiwan.Domain.Entities.Organization;

/// <summary>
/// The institution's logo image, kept apart from <see cref="InstitutionSetting"/> so that reading the
/// settings (which many handlers do) never loads the image bytes. One row per setting. The image kind is
/// decided from the bytes themselves, never from what the client claims.
/// </summary>
public sealed class InstitutionLogo
{
    public const int MaxBytes = 1024 * 1024;

    public Guid SettingId { get; private set; }
    public string ContentType { get; private set; } = null!;
    public byte[] Data { get; private set; } = null!;
    public DateTime UpdatedAt { get; private set; }

    private InstitutionLogo()
    {
    }

    public static Result<InstitutionLogo> Create(Guid settingId, byte[] data, DateTime utcNow)
    {
        var checkedType = Inspect(data);
        if (checkedType.IsFailure)
            return Result.Failure<InstitutionLogo>(checkedType.Error);

        return Result.Success(new InstitutionLogo
        {
            SettingId = settingId,
            ContentType = checkedType.Value,
            Data = data,
            UpdatedAt = utcNow
        });
    }

    /// <summary>Swaps the image for another one that passes the same checks.</summary>
    public Result Replace(byte[] data, DateTime utcNow)
    {
        var checkedType = Inspect(data);
        if (checkedType.IsFailure)
            return Result.Failure(checkedType.Error);

        ContentType = checkedType.Value;
        Data = data;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    /// <summary>The media type of an acceptable logo, or why the bytes are refused.</summary>
    public static Result<string> Inspect(byte[]? data)
    {
        if (data is null || data.Length == 0)
            return Result.Failure<string>(DomainErrors.InstitutionSetting.LogoEmpty);

        if (data.Length > MaxBytes)
            return Result.Failure<string>(DomainErrors.InstitutionSetting.LogoTooLarge);

        if (data.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return Result.Success("image/png");

        if (data.AsSpan().StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
            return Result.Success("image/jpeg");

        if (data.Length > 12 &&
            data.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            data.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return Result.Success("image/webp");

        return IsSafeSvg(data)
            ? Result.Success("image/svg+xml")
            : Result.Failure<string>(DomainErrors.InstitutionSetting.LogoTypeNotAllowed);
    }

    /// <summary>
    /// An SVG is markup, so it is accepted only when it is plainly an image: it opens an svg element and
    /// carries nothing that runs (scripts, event handlers, script URLs, embedded foreign content).
    /// </summary>
    private static bool IsSafeSvg(byte[] data)
    {
        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(data).TrimStart('﻿');
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (text.IndexOf("<svg", StringComparison.OrdinalIgnoreCase) < 0)
            return false;

        var head = text.TrimStart();
        if (!head.StartsWith("<", StringComparison.Ordinal))
            return false;

        string[] forbidden = { "<script", "javascript:", "<foreignobject", "<iframe", "<!entity", "<!doctype html" };
        if (forbidden.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            return false;

        // Event-handler attributes such as onload= or onclick=.
        for (var index = text.IndexOf("on", StringComparison.OrdinalIgnoreCase);
             index >= 0;
             index = text.IndexOf("on", index + 2, StringComparison.OrdinalIgnoreCase))
        {
            if (index == 0 || !char.IsWhiteSpace(text[index - 1]) && text[index - 1] != '"' && text[index - 1] != '\'')
                continue;

            var cursor = index + 2;
            while (cursor < text.Length && char.IsLetter(text[cursor]))
                cursor++;
            while (cursor < text.Length && char.IsWhiteSpace(text[cursor]))
                cursor++;
            if (cursor < text.Length && text[cursor] == '=' && cursor > index + 2)
                return false;
        }

        return true;
    }
}
