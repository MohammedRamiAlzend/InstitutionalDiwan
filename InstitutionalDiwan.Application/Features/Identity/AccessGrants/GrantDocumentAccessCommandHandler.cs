using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.AccessGrants;

internal sealed class GrantDocumentAccessCommandHandler
    : ICommandHandler<GrantDocumentAccessCommand, DocumentAccessGrantResponse>
{
    private readonly IDocumentAccessGrantRepository _grantRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GrantDocumentAccessCommandHandler(
        IDocumentAccessGrantRepository grantRepository,
        IDocumentRepository documentRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _grantRepository = grantRepository;
        _documentRepository = documentRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentAccessGrantResponse>> Handle(
        GrantDocumentAccessCommand request,
        CancellationToken cancellationToken)
    {
        if (await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken) is null)
            return Result.Failure<DocumentAccessGrantResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (await _userRepository.GetByIdAsync(request.UserId, cancellationToken) is null)
            return Result.Failure<DocumentAccessGrantResponse>(DomainErrors.Identity.UserNotFound);

        var grantResult = DocumentAccessGrant.Grant(
            request.DocumentId,
            request.UserId,
            request.AccessLevelId,
            request.GrantedByUserId,
            request.Reason,
            request.ExpiresAt,
            utcNow: _unitOfWork.UtcNow);

        if (grantResult.IsFailure)
            return Result.Failure<DocumentAccessGrantResponse>(grantResult.Error);

        var grant = grantResult.Value;

        _grantRepository.Add(grant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentAccessGrantResponse.From(grant);
    }
}
