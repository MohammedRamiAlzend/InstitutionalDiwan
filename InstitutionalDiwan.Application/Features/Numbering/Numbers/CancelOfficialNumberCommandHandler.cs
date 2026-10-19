using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

internal sealed class CancelOfficialNumberCommandHandler
    : ICommandHandler<CancelOfficialNumberCommand, DocumentNumberResponse>
{
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOfficialNumberCommandHandler(
        IDocumentNumberRepository documentNumberRepository,
        IUnitOfWork unitOfWork)
    {
        _documentNumberRepository = documentNumberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentNumberResponse>> Handle(
        CancelOfficialNumberCommand request,
        CancellationToken cancellationToken)
    {
        var number = await _documentNumberRepository.GetByIdAsync(
            request.DocumentNumberId, cancellationToken);

        if (number is null || number.DocumentId != request.DocumentId)
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.NumberNotFound);

        if (number.StatusId != DocumentNumberStatus.ActiveId)
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.NumberNotActive);

        var cancelResult = number.Cancel(request.UserId, request.Reason);
        if (cancelResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(cancelResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentNumberResponse.From(number);
    }
}
