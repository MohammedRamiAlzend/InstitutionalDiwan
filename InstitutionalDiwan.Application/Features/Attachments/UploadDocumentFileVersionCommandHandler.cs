using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Attachments;

internal sealed class UploadDocumentFileVersionCommandHandler
    : ICommandHandler<UploadDocumentFileVersionCommand, DocumentFileResponse>
{
    private readonly IDocumentFileRepository _fileRepository;
    private readonly IDocumentFileTypeRepository _fileTypeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly AttachmentUploader _uploader;
    private readonly IUnitOfWork _unitOfWork;

    public UploadDocumentFileVersionCommandHandler(
        IDocumentFileRepository fileRepository,
        IDocumentFileTypeRepository fileTypeRepository,
        IUserRepository userRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        AttachmentUploader uploader,
        IUnitOfWork unitOfWork)
    {
        _fileRepository = fileRepository;
        _fileTypeRepository = fileTypeRepository;
        _userRepository = userRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _uploader = uploader;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentFileResponse>> Handle(
        UploadDocumentFileVersionCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<DocumentFileResponse>(DomainErrors.Identity.UserNotFound);

        if (!user.IsActive)
            return Result.Failure<DocumentFileResponse>(DomainErrors.Identity.UserInactive);

        var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
        if (file is null)
            return Result.Failure<DocumentFileResponse>(DomainErrors.Attachments.FileNotFound);

        if (file.IsArchived)
            return Result.Failure<DocumentFileResponse>(DomainErrors.Attachments.FileArchived);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                file.DocumentId, AccessLevel.ManageId, cancellationToken))
        {
            return Result.Failure<DocumentFileResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var fileType = await _fileTypeRepository.GetByIdAsync(file.FileTypeId, cancellationToken);
        if (fileType is null)
            return Result.Failure<DocumentFileResponse>(DomainErrors.Attachments.FileTypeNotFound);

        if (!fileType.IsActive)
            return Result.Failure<DocumentFileResponse>(DomainErrors.Attachments.FileTypeInactive);

        var storedResult = await _uploader.StoreAsync(
            file.DocumentId,
            fileType,
            request.Content,
            request.OriginalFileName,
            request.MimeType,
            request.FileSizeBytes,
            cancellationToken);
        if (storedResult.IsFailure)
            return Result.Failure<DocumentFileResponse>(storedResult.Error);

        var stored = storedResult.Value;
        var versionResult = file.AddVersion(
            stored.StorageProviderId,
            stored.RelativePath,
            stored.OriginalFileName,
            stored.MimeType,
            stored.FileSizeBytes,
            stored.ChecksumSha256,
            request.UserId,
            request.ChangeNotes,
            _unitOfWork.UtcNow);

        if (versionResult.IsFailure)
            return Result.Failure<DocumentFileResponse>(versionResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentFileResponse.From(file);
    }
}
