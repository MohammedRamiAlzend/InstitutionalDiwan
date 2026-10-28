using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.AccessGrants;

internal sealed class RevokeDocumentAccessCommandHandler
    : ICommandHandler<RevokeDocumentAccessCommand, DocumentAccessGrantResponse>
{
    private readonly IDocumentAccessGrantRepository _grantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeDocumentAccessCommandHandler(
        IDocumentAccessGrantRepository grantRepository,
        IUnitOfWork unitOfWork)
    {
        _grantRepository = grantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentAccessGrantResponse>> Handle(
        RevokeDocumentAccessCommand request,
        CancellationToken cancellationToken)
    {
        var grant = await _grantRepository.GetByIdAsync(request.GrantId, cancellationToken);
        if (grant is null || grant.DocumentId != request.DocumentId)
            return Result.Failure<DocumentAccessGrantResponse>(DomainErrors.Identity.AccessGrantNotFound);

        var revokeResult = grant.Revoke(request.RevokedByUserId, _unitOfWork.UtcNow);
        if (revokeResult.IsFailure)
            return Result.Failure<DocumentAccessGrantResponse>(revokeResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentAccessGrantResponse.From(grant);
    }
}
