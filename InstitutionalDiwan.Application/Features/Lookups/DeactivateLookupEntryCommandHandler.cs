using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Lookups;

internal sealed class DeactivateLookupEntryCommandHandler
    : ICommandHandler<DeactivateLookupEntryCommand, LookupEntryResponse>
{
    private readonly ILookupRepository _lookupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateLookupEntryCommandHandler(
        ILookupRepository lookupRepository,
        IUnitOfWork unitOfWork)
    {
        _lookupRepository = lookupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LookupEntryResponse>> Handle(
        DeactivateLookupEntryCommand request,
        CancellationToken cancellationToken)
    {
        if (!_lookupRepository.IsSupported(request.Lookup))
            return Result.Failure<LookupEntryResponse>(DomainErrors.Lookup.UnknownCatalog(request.Lookup));

        if (await _lookupRepository.IsInUseAsync(request.Lookup, request.Id, cancellationToken))
            return Result.Failure<LookupEntryResponse>(
                DomainErrors.Lookup.EntryInUse(request.Lookup, request.Id));

        var entry = await _lookupRepository.SetActiveStateAsync(
            request.Lookup, request.Id, isActive: false, cancellationToken);

        if (entry is null)
            return Result.Failure<LookupEntryResponse>(
                DomainErrors.Lookup.EntryNotFound(request.Lookup, request.Id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return LookupEntryResponse.From(entry);
    }
}
