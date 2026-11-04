using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Numbering;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

internal sealed class RecordDepartmentRegistrationCommandHandler
    : ICommandHandler<RecordDepartmentRegistrationCommand, DocumentNumberResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly INumberingRegistryRepository _numberingRegistryRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUnitOfWork _unitOfWork;

    public RecordDepartmentRegistrationCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository,
        IDepartmentRepository departmentRepository,
        INumberingRegistryRepository numberingRegistryRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
        _departmentRepository = departmentRepository;
        _numberingRegistryRepository = numberingRegistryRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _unitOfWork = unitOfWork;
    }

    private async Task<Result<NumberingRegistry>> ResolveRegistryAsync(
        RecordDepartmentRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RegistryId is not { } registryId)
        {
            return await NumberingRegistryResolver.RequireDepartmentRegistryAsync(
                _numberingRegistryRepository, request.DepartmentId, cancellationToken);
        }

        var chosen = await _numberingRegistryRepository.GetByIdAsync(registryId, cancellationToken);
        if (chosen is null)
            return Result.Failure<NumberingRegistry>(DomainErrors.Numbering.RegistryNotFound);

        if (!chosen.IsActive || chosen.DepartmentId != request.DepartmentId)
            return Result.Failure<NumberingRegistry>(DomainErrors.Numbering.DepartmentRegistryMismatch);

        return Result.Success(chosen);
    }

    public async Task<Result<DocumentNumberResponse>> Handle(
        RecordDepartmentRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetWithNumbersAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _departmentScopeEvaluator.CanAccessDepartmentAsync(request.DepartmentId, cancellationToken))
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Correspondence.DepartmentOutOfScope);

        if (!await _departmentScopeEvaluator.IsPrimaryDepartmentAsync(request.DepartmentId, cancellationToken))
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.DepartmentRegistrationOwnDepartmentOnly);

        var notArchived = document.EnsureNotArchived();
        if (notArchived.IsFailure)
            return Result.Failure<DocumentNumberResponse>(notArchived.Error);

        if (await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken) is null)
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Department.NotFound);

        var registryResult = await ResolveRegistryAsync(request, cancellationToken);

        if (registryResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(registryResult.Error);

        var registry = registryResult.Value;
        var numberText = request.RegistryNumberText?.Trim() ?? string.Empty;
        var numberingYear = request.RegistryDate.Year;

        var existing = document.FindDepartmentNumber(request.DepartmentId);

        if (existing is not null)
        {
            if (!string.Equals(existing.OfficialNumberText, numberText, StringComparison.OrdinalIgnoreCase))
                return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.DepartmentNumberLocked);

            var reRegister = existing.ReRegisterInDepartment(
                request.UserId, request.RegistryDate, request.Notes, _unitOfWork.UtcNow);

            if (reRegister.IsFailure)
                return Result.Failure<DocumentNumberResponse>(reRegister.Error);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return DocumentNumberResponse.From(existing);
        }

        if (document.ActiveOfficialNumberId is null)
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.DocumentNotRegistered);

        if (string.IsNullOrWhiteSpace(numberText))
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.DepartmentNumberRequired);

        if (await _documentNumberRepository.ExistsInScopeAsync(
                DocumentNumberScope.DepartmentId, registry.RegistryId, numberText, numberingYear, cancellationToken))
        {
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.RegistryNumberTaken);
        }

        var entryResult = DocumentNumber.CreateDepartmentEntry(
            document.DocumentId,
            request.DepartmentId,
            registry.RegistryId,
            numberText,
            request.RegistryDate,
            request.UserId,
            request.Notes,
            utcNow: _unitOfWork.UtcNow);

        if (entryResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(entryResult.Error);

        var attachResult = document.AddDepartmentNumber(entryResult.Value, _unitOfWork.UtcNow);
        if (attachResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(attachResult.Error);

        _documentNumberRepository.Add(entryResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentNumberResponse.From(entryResult.Value);
    }
}
