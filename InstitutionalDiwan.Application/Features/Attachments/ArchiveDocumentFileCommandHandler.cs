using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Attachments;

internal sealed class ArchiveDocumentFileCommandHandler
    : ICommandHandler<ArchiveDocumentFileCommand, DocumentFileResponse>
{
    private readonly IDocumentFileRepository _fileRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;

    public ArchiveDocumentFileCommandHandler(
        IDocumentFileRepository fileRepository,
        IUserRepository userRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IUnitOfWork unitOfWork)
    {
        _fileRepository = fileRepository;
        _userRepository = userRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentFileResponse>> Handle(
        ArchiveDocumentFileCommand request,
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

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                file.DocumentId, AccessLevel.ManageId, cancellationToken))
        {
            return Result.Failure<DocumentFileResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var archiveResult = file.Archive(_unitOfWork.UtcNow);
        if (archiveResult.IsFailure)
            return Result.Failure<DocumentFileResponse>(archiveResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentFileResponse.From(file);
    }
}
