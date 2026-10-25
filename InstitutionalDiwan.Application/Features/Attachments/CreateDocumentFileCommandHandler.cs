using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Attachments;

internal sealed class CreateDocumentFileCommandHandler
    : ICommandHandler<CreateDocumentFileCommand, DocumentFileResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentFileTypeRepository _fileTypeRepository;
    private readonly IDocumentFileRepository _fileRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDocumentFileCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentFileTypeRepository fileTypeRepository,
        IDocumentFileRepository fileRepository,
        IUserRepository userRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _fileTypeRepository = fileTypeRepository;
        _fileRepository = fileRepository;
        _userRepository = userRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentFileResponse>> Handle(
        CreateDocumentFileCommand request,
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
        _fileRepository.Add(file);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentFileResponse.From(file);
    }
}
