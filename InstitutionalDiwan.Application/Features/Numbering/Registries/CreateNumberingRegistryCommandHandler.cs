using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

internal sealed class CreateNumberingRegistryCommandHandler
    : ICommandHandler<CreateNumberingRegistryCommand, NumberingRegistryResponse>
{
    private readonly INumberingRegistryRepository _registryRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateNumberingRegistryCommandHandler(
        INumberingRegistryRepository registryRepository,
        IDepartmentRepository departmentRepository,
        IDocumentTypeRepository documentTypeRepository,
        ILookupRepository lookupRepository,
        IUnitOfWork unitOfWork)
    {
        _registryRepository = registryRepository;
        _departmentRepository = departmentRepository;
        _documentTypeRepository = documentTypeRepository;
        _lookupRepository = lookupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<NumberingRegistryResponse>> Handle(
        CreateNumberingRegistryCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedCode = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedCode.Length == 0)
            return Result.Failure<NumberingRegistryResponse>(DomainErrors.Numbering.RegistryCodeEmpty);

        var existing = await _registryRepository.ListAllAsync(cancellationToken);
        if (existing.Any(registry => string.Equals(
                registry.Code, normalizedCode, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<NumberingRegistryResponse>(
                DomainErrors.Numbering.RegistryCodeAlreadyExists);
        }

        var scopeResult = await NumberingRegistryValidation.ValidateScopeAsync(
            request.DepartmentId,
            request.DocumentTypeId,
            request.DirectionId,
            _departmentRepository,
            _documentTypeRepository,
            cancellationToken);
        if (scopeResult.IsFailure)
            return Result.Failure<NumberingRegistryResponse>(scopeResult.Error);

        var resetPolicyId = NumberingRegistryValidation.NormalizeResetPolicy(request.ResetPolicyId);
        var policyResult = await NumberingRegistryValidation.ValidateResetPolicyAsync(
            resetPolicyId, _lookupRepository, cancellationToken);
        if (policyResult.IsFailure)
            return Result.Failure<NumberingRegistryResponse>(policyResult.Error);

        var createResult = NumberingRegistry.Create(
            normalizedCode,
            request.NameAr,
            request.FormatPattern ?? OfficialNumberFormatter.DefaultPattern,
            resetPolicyId,
            request.DepartmentId,
            request.DocumentTypeId,
            request.DirectionId);

        if (createResult.IsFailure)
            return Result.Failure<NumberingRegistryResponse>(createResult.Error);

        _registryRepository.Add(createResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NumberingRegistryResponse.From(createResult.Value);
    }
}
