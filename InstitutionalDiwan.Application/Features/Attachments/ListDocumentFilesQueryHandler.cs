using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Attachments;

internal sealed class ListDocumentFilesQueryHandler
    : IQueryHandler<ListDocumentFilesQuery, IReadOnlyCollection<DocumentFileResponse>>
{
    private readonly IDocumentFileRepository _fileRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;

    public ListDocumentFilesQueryHandler(
        IDocumentFileRepository fileRepository,
        IUserRepository userRepository,
        IDocumentAccessEvaluator documentAccessEvaluator)
    {
        _fileRepository = fileRepository;
        _userRepository = userRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<Result<IReadOnlyCollection<DocumentFileResponse>>> Handle(
        ListDocumentFilesQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<IReadOnlyCollection<DocumentFileResponse>>(
                DomainErrors.Identity.UserNotFound);

        if (!user.IsActive)
            return Result.Failure<IReadOnlyCollection<DocumentFileResponse>>(
                DomainErrors.Identity.UserInactive);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.ViewId, cancellationToken))
        {
            return Result.Failure<IReadOnlyCollection<DocumentFileResponse>>(
                DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var files = await _fileRepository.ListByDocumentAsync(request.DocumentId, cancellationToken);

        return Result.Success<IReadOnlyCollection<DocumentFileResponse>>(
            files.Select(DocumentFileResponse.From).ToList());
    }
}
