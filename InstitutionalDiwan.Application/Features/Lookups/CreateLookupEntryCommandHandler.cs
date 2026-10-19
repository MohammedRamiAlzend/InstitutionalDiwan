using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Lookups;

internal sealed class CreateLookupEntryCommandHandler
    : ICommandHandler<CreateLookupEntryCommand, LookupEntryResponse>
{
    private readonly ILookupRepository _lookupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateLookupEntryCommandHandler(
        ILookupRepository lookupRepository,
        IUnitOfWork unitOfWork)
    {
        _lookupRepository = lookupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LookupEntryResponse>> Handle(
        CreateLookupEntryCommand request,
        CancellationToken cancellationToken)
    {
        if (!_lookupRepository.IsSupported(request.Lookup))
            return Result.Failure<LookupEntryResponse>(DomainErrors.Lookup.UnknownCatalog(request.Lookup));

        var addResult = await _lookupRepository.AddAsync(
            request.Lookup, request.Id, request.Desc, request.DescAr, cancellationToken);

        if (addResult.IsFailure)
            return Result.Failure<LookupEntryResponse>(addResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return LookupEntryResponse.From(addResult.Value);
    }
}
