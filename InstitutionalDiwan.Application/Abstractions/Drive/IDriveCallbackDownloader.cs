namespace InstitutionalDiwan.Application.Abstractions.Drive;

/// <summary>
/// Downloads the edited document bytes from the temporary URL OnlyOffice supplies in its save
/// callback (Module 11). Returns a rewound, seekable stream, or null when the download fails.
/// </summary>
public interface IDriveCallbackDownloader
{
    Task<Stream?> DownloadAsync(string url, CancellationToken cancellationToken = default);
}