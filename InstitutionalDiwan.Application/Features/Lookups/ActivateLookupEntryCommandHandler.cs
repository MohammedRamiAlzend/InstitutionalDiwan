using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Lookups;

internal sealed class ActivateLookupEntryCommandHandler
    : ICommandHandler<ActivateLookupEntryCommand, LookupEntryResponse>
{
    private readonly ILookupRepository _lookupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateLookupEntryCommandHandler(
        ILookupRepository lookupRepository,
        IUnitOfWork unitOfWork)
    {
        _lookupRepository = lookupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LookupEntryResponse>> Handle(
        ActivateLookupEntryCommand request,
        CancellationToken cancellationToken)
    {
        if (!_lookupRepository.IsSupported(request.Lookup))
            return Result.Failure<LookupEntryResponse>(DomainErrors.Lookup.UnknownCatalog(request.Lookup));

        var entry = await _lookupRepository.SetActiveStateAsync(
            request.Lookup, request.Id, isActive: true, cancellationToken);

        if (entry is null)
            return Result.Failure<LookupEntryResponse>(
                DomainErrors.Lookup.EntryNotFound(request.Lookup, request.Id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return LookupEntryResponse.From(entry);
    }
}
