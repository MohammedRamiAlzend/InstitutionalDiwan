using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

internal sealed class RecordDepartmentRegistrationCommandHandler
    : ICommandHandler<RecordDepartmentRegistrationCommand, DocumentNumberResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly DepartmentRegistrationService _registrationService;
    private readonly IUnitOfWork _unitOfWork;

    public RecordDepartmentRegistrationCommandHandler(
        IDocumentRepository documentRepository,
        DepartmentRegistrationService registrationService,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _registrationService = registrationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentNumberResponse>> Handle(
        RecordDepartmentRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetWithNumbersAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Correspondence.DocumentNotFound);

        var canRegister = await _registrationService.EnsureCanRegisterAsync(
            document, request.DepartmentId, cancellationToken);
        if (canRegister.IsFailure)
            return Result.Failure<DocumentNumberResponse>(canRegister.Error);

        var registryResult = await _registrationService.ResolveRegistryAsync(
            request.DepartmentId, request.RegistryId, cancellationToken);
        if (registryResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(registryResult.Error);

        var numberText = request.RegistryNumberText?.Trim() ?? string.Empty;
        var existing = document.FindDepartmentNumber(request.DepartmentId, request.RegistryDate.Year);

        if (existing is not null)
        {
            if (!string.Equals(existing.OfficialNumberText, numberText, StringComparison.OrdinalIgnoreCase))
                return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.DepartmentNumberLocked);

            var visit = existing.RecordVisit(
                request.UserId, request.RegistryDate, notes: request.Notes, utcNow: _unitOfWork.UtcNow);
            if (visit.IsFailure)
                return Result.Failure<DocumentNumberResponse>(visit.Error);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return DocumentNumberResponse.From(existing);
        }

        var entryResult = await _registrationService.CreateEntryAsync(
            document,
            request.DepartmentId,
            registryResult.Value,
            numberText,
            request.RegistryDate,
            request.UserId,
            request.Notes,
            movementStepId: null,
            cancellationToken);
        if (entryResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(entryResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return DocumentNumberResponse.From(entryResult.Value);
    }
}
