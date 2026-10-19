using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Lookups;

internal sealed class RelabelLookupEntryCommandHandler
    : ICommandHandler<RelabelLookupEntryCommand, LookupEntryResponse>
{
    private readonly ILookupRepository _lookupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RelabelLookupEntryCommandHandler(
        ILookupRepository lookupRepository,
        IUnitOfWork unitOfWork)
    {
        _lookupRepository = lookupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LookupEntryResponse>> Handle(
        RelabelLookupEntryCommand request,
        CancellationToken cancellationToken)
    {
        if (!_lookupRepository.IsSupported(request.Lookup))
            return Result.Failure<LookupEntryResponse>(DomainErrors.Lookup.UnknownCatalog(request.Lookup));

        var entry = await _lookupRepository.RelabelAsync(
            request.Lookup, request.Id, request.Desc, request.DescAr, cancellationToken);

        if (entry is null)
            return Result.Failure<LookupEntryResponse>(
                DomainErrors.Lookup.EntryNotFound(request.Lookup, request.Id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return LookupEntryResponse.From(entry);
    }
}
