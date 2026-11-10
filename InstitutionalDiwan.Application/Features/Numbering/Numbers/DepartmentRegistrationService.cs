using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

/// <summary>
/// The rules for writing a document into a department's register, shared by the registration
/// endpoint and by a receipt that carries a new number. Nothing here saves; the caller owns the
/// unit of work so a receipt and its register line land together.
/// </summary>
internal sealed class DepartmentRegistrationService
{
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly INumberingRegistryRepository _numberingRegistryRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentRegistrationService(
        IDocumentNumberRepository documentNumberRepository,
        IDepartmentRepository departmentRepository,
        INumberingRegistryRepository numberingRegistryRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUnitOfWork unitOfWork)
    {
        _documentNumberRepository = documentNumberRepository;
        _departmentRepository = departmentRepository;
        _numberingRegistryRepository = numberingRegistryRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Only the caller's own department may write in its register, and never on an archived document.</summary>
    public async Task<Result> EnsureCanRegisterAsync(
        Document document,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        if (!await _departmentScopeEvaluator.CanAccessDepartmentAsync(departmentId, cancellationToken))
            return Result.Failure(DomainErrors.Correspondence.DepartmentOutOfScope);

        if (!await _departmentScopeEvaluator.IsPrimaryDepartmentAsync(departmentId, cancellationToken))
            return Result.Failure(DomainErrors.Numbering.DepartmentRegistrationOwnDepartmentOnly);

        var notArchived = document.EnsureNotArchived();
        if (notArchived.IsFailure)
            return notArchived;

        return await _departmentRepository.GetByIdAsync(departmentId, cancellationToken) is null
            ? Result.Failure(DomainErrors.Department.NotFound)
            : Result.Success();
    }

    /// <summary>The chosen register, or the department's best active register when none is chosen.</summary>
    public async Task<Result<NumberingRegistry>> ResolveRegistryAsync(
        Guid departmentId,
        Guid? registryId,
        CancellationToken cancellationToken)
    {
        if (registryId is not { } chosenId)
        {
            return await NumberingRegistryResolver.RequireDepartmentRegistryAsync(
                _numberingRegistryRepository, departmentId, cancellationToken);
        }

        var chosen = await _numberingRegistryRepository.GetByIdAsync(chosenId, cancellationToken);
        if (chosen is null)
            return Result.Failure<NumberingRegistry>(DomainErrors.Numbering.RegistryNotFound);

        if (!chosen.IsActive || chosen.DepartmentId != departmentId)
            return Result.Failure<NumberingRegistry>(DomainErrors.Numbering.DepartmentRegistryMismatch);

        return Result.Success(chosen);
    }

    /// <summary>
    /// Creates the department's register line for the year of <paramref name="registryDate"/>, with its
    /// first visit. With <paramref name="replaceCurrent"/> the department is renumbering a document it
    /// already registered that year: its current line becomes <c>REPLACED</c> and links to the new one.
    /// </summary>
    public async Task<Result<DocumentNumber>> CreateEntryAsync(
        Document document,
        Guid departmentId,
        NumberingRegistry registry,
        string? registryNumberText,
        DateTime registryDate,
        Guid userId,
        string? notes,
        Guid? movementStepId,
        CancellationToken cancellationToken,
        bool replaceCurrent = false)
    {
        if (document.ActiveOfficialNumberId is null)
            return Result.Failure<DocumentNumber>(DomainErrors.Numbering.DocumentNotRegistered);

        var numberText = registryNumberText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(numberText))
            return Result.Failure<DocumentNumber>(DomainErrors.Numbering.DepartmentNumberRequired);

        if (await _documentNumberRepository.ExistsInScopeAsync(
                DocumentNumberScope.DepartmentId, registry.RegistryId, numberText, registryDate.Year, cancellationToken))
        {
            return Result.Failure<DocumentNumber>(DomainErrors.Numbering.RegistryNumberTaken);
        }

        var entryResult = DocumentNumber.CreateDepartmentEntry(
            document.DocumentId,
            departmentId,
            registry.RegistryId,
            numberText,
            registryDate,
            userId,
            notes,
            utcNow: _unitOfWork.UtcNow,
            movementStepId: movementStepId);

        if (entryResult.IsFailure)
            return entryResult;

        var attachResult = replaceCurrent
            ? document.ReplaceDepartmentNumber(entryResult.Value, userId, notes, _unitOfWork.UtcNow)
            : document.AddDepartmentNumber(entryResult.Value, _unitOfWork.UtcNow);
        if (attachResult.IsFailure)
            return Result.Failure<DocumentNumber>(attachResult.Error);

        _documentNumberRepository.Add(entryResult.Value);
        return entryResult;
    }
}
