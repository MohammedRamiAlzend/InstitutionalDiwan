using InstitutionalDiwan.Application.Abstractions.Drive;

namespace InstitutionalDiwan.Infrastructure.OnlyOffice;

/// <summary>
/// Downloads the edited bytes from the temporary URL OnlyOffice supplies in its save callback
/// (Module 11). A single shared <see cref="HttpClient"/> is reused; the response is buffered into a
/// rewound, seekable stream so the caller can hash it and upload it to SeaweedFS.
/// </summary>
public sealed class DriveCallbackDownloader : IDriveCallbackDownloader
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(2) };

    public async Task<Stream?> DownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;

        try
        {
            using var response = await Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var buffer = new MemoryStream();
            await response.Content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            return buffer;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or IOException)
        {
            return null;
        }
    }
}