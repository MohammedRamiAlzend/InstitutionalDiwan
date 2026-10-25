using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Attachments;

internal sealed class CreateDocumentFileWithVersionCommandHandler
    : ICommandHandler<CreateDocumentFileWithVersionCommand, DocumentFileResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentFileTypeRepository _fileTypeRepository;
    private readonly IDocumentFileRepository _fileRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly AttachmentUploader _uploader;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDocumentFileWithVersionCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentFileTypeRepository fileTypeRepository,
        IDocumentFileRepository fileRepository,
        IUserRepository userRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        AttachmentUploader uploader,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _fileTypeRepository = fileTypeRepository;
        _fileRepository = fileRepository;
        _userRepository = userRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _uploader = uploader;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentFileResponse>> Handle(
        CreateDocumentFileWithVersionCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<DocumentFileResponse>(DomainErrors.Identity.UserNotFound);

        if (!user.IsActive)
            return Result.Failure<DocumentFileResponse>(DomainErrors.Identity.UserInactive);

        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentFileResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.ManageId, cancellationToken))
        {
            return Result.Failure<DocumentFileResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var fileType = await _fileTypeRepository.GetByIdAsync(request.FileTypeId, cancellationToken);
        if (fileType is null)
            return Result.Failure<DocumentFileResponse>(DomainErrors.Attachments.FileTypeNotFound);

        if (!fileType.IsActive)
            return Result.Failure<DocumentFileResponse>(DomainErrors.Attachments.FileTypeInactive);

        var fileResult = DocumentFile.Create(
            request.DocumentId,
            request.FileTypeId,
            request.Title,
            request.UserId,
            request.IsPrimaryScan,
            _unitOfWork.UtcNow);

        if (fileResult.IsFailure)
            return Result.Failure<DocumentFileResponse>(fileResult.Error);

        var file = fileResult.Value;

        var storedResult = await _uploader.StoreAsync(
            request.DocumentId,
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

        _fileRepository.Add(file);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentFileResponse.From(file);
    }
}
