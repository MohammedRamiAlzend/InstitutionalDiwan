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
    private readonly IUnitOfWork _unitOfWork;

    public RecordDepartmentRegistrationCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository,
        IDepartmentRepository departmentRepository,
        INumberingRegistryRepository numberingRegistryRepository,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
        _departmentRepository = departmentRepository;
        _numberingRegistryRepository = numberingRegistryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentNumberResponse>> Handle(
        RecordDepartmentRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetWithNumbersAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken) is null)
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Department.NotFound);

        var registryResult = await NumberingRegistryResolver.RequireDepartmentRegistryAsync(
            _numberingRegistryRepository, request.DepartmentId, cancellationToken);

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
                request.UserId, request.RegistryDate, request.Notes);

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
            request.Notes);

        if (entryResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(entryResult.Error);

        var attachResult = document.AddDepartmentNumber(entryResult.Value);
        if (attachResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(attachResult.Error);

        _documentNumberRepository.Add(entryResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentNumberResponse.From(entryResult.Value);
    }
}
