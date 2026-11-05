using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Lookups;

internal sealed class SetLookupEntryDefaultCommandHandler
    : ICommandHandler<SetLookupEntryDefaultCommand, LookupEntryResponse>
{
    private readonly ILookupRepository _lookupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetLookupEntryDefaultCommandHandler(
        ILookupRepository lookupRepository,
        IUnitOfWork unitOfWork)
    {
        _lookupRepository = lookupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LookupEntryResponse>> Handle(
        SetLookupEntryDefaultCommand request,
        CancellationToken cancellationToken)
    {
        if (!_lookupRepository.IsSupported(request.Lookup))
            return Result.Failure<LookupEntryResponse>(DomainErrors.Lookup.UnknownCatalog(request.Lookup));

        var result = await _lookupRepository.SetDefaultStateAsync(
            request.Lookup, request.Id, request.IsDefault, cancellationToken);

        if (result.IsFailure)
            return Result.Failure<LookupEntryResponse>(result.Error);

        if (result.Value is null)
            return Result.Failure<LookupEntryResponse>(
                DomainErrors.Lookup.EntryNotFound(request.Lookup, request.Id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return LookupEntryResponse.From(result.Value);
    }
}
