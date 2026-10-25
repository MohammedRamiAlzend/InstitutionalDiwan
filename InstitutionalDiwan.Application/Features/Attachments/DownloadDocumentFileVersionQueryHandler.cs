using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Attachments;

internal sealed class DownloadDocumentFileVersionQueryHandler
    : IQueryHandler<DownloadDocumentFileVersionQuery, DocumentFileDownload>
{
    private readonly IDocumentFileRepository _fileRepository;
    private readonly IFileStorageProviderRepository _providerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IFileStorageService _storage;

    public DownloadDocumentFileVersionQueryHandler(
        IDocumentFileRepository fileRepository,
        IFileStorageProviderRepository providerRepository,
        IUserRepository userRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IFileStorageService storage)
    {
        _fileRepository = fileRepository;
        _providerRepository = providerRepository;
        _userRepository = userRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _storage = storage;
    }

    public async Task<Result<DocumentFileDownload>> Handle(
        DownloadDocumentFileVersionQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<DocumentFileDownload>(DomainErrors.Identity.UserNotFound);

        if (!user.IsActive)
            return Result.Failure<DocumentFileDownload>(DomainErrors.Identity.UserInactive);

        var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
        if (file is null)
            return Result.Failure<DocumentFileDownload>(DomainErrors.Attachments.FileNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                file.DocumentId, AccessLevel.DownloadId, cancellationToken))
        {
            return Result.Failure<DocumentFileDownload>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var version = FindVersion(file, request.VersionId);
        if (version is null)
            return Result.Failure<DocumentFileDownload>(DomainErrors.Attachments.VersionNotFound);

        var provider = await _providerRepository.GetByIdAsync(
            version.StorageProviderId, cancellationToken);
        if (provider is null)
            return Result.Failure<DocumentFileDownload>(DomainErrors.Attachments.StorageProviderNotFound);

        var content = await _storage.OpenReadAsync(
            provider.BasePathOrEndpoint, version.RelativePath, cancellationToken);
        if (content is null)
            return Result.Failure<DocumentFileDownload>(DomainErrors.Attachments.FileNotFound);

        return Result.Success(new DocumentFileDownload(
            content,
            version.OriginalFileName,
            version.MimeType,
            version.FileSizeBytes));
    }

    private static DocumentFileVersion? FindVersion(DocumentFile file, Guid? versionId) =>
        versionId is { } id
            ? file.Versions.FirstOrDefault(version => version.VersionId == id)
            : file.Versions.FirstOrDefault(version => version.IsCurrent);
}
