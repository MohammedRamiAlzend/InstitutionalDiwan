using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

internal sealed class UpdateNumberingRegistryCommandHandler
    : ICommandHandler<UpdateNumberingRegistryCommand, NumberingRegistryResponse>
{
    private readonly INumberingRegistryRepository _registryRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateNumberingRegistryCommandHandler(
        INumberingRegistryRepository registryRepository,
        IDocumentNumberRepository documentNumberRepository,
        IDepartmentRepository departmentRepository,
        IDocumentTypeRepository documentTypeRepository,
        ILookupRepository lookupRepository,
        IUnitOfWork unitOfWork)
    {
        _registryRepository = registryRepository;
        _documentNumberRepository = documentNumberRepository;
        _departmentRepository = departmentRepository;
        _documentTypeRepository = documentTypeRepository;
        _lookupRepository = lookupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<NumberingRegistryResponse>> Handle(
        UpdateNumberingRegistryCommand request,
        CancellationToken cancellationToken)
    {
        var registry = await _registryRepository.GetByIdAsync(request.RegistryId, cancellationToken);
        if (registry is null)
            return Result.Failure<NumberingRegistryResponse>(DomainErrors.Numbering.RegistryNotFound);

        var scopeResult = await NumberingRegistryValidation.ValidateScopeAsync(
            request.DepartmentId,
            request.DocumentTypeId,
            request.DirectionId,
            _departmentRepository,
            _documentTypeRepository,
            cancellationToken);
        if (scopeResult.IsFailure)
            return Result.Failure<NumberingRegistryResponse>(scopeResult.Error);

        var resetPolicyId = NumberingRegistryValidation.NormalizeResetPolicy(
            request.ResetPolicyId > 0 ? request.ResetPolicyId : registry.ResetPolicyId);

        var policyResult = await NumberingRegistryValidation.ValidateResetPolicyAsync(
            resetPolicyId, _lookupRepository, cancellationToken);
        if (policyResult.IsFailure)
            return Result.Failure<NumberingRegistryResponse>(policyResult.Error);

        var hasIssuedNumbers = await _documentNumberRepository.AnyByRegistryAsync(
            registry.RegistryId, cancellationToken);

        var updateResult = registry.Update(
            request.NameAr,
            request.FormatPattern,
            resetPolicyId,
            request.DepartmentId,
            request.DocumentTypeId,
            request.DirectionId,
            hasIssuedNumbers);

        if (updateResult.IsFailure)
            return Result.Failure<NumberingRegistryResponse>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NumberingRegistryResponse.From(registry);
    }
}
