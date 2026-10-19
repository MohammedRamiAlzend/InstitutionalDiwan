using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Numbering;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class RegisterDocumentCommandHandler
    : ICommandHandler<RegisterDocumentCommand, DocumentResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly INumberingRegistryRepository _numberingRegistryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository,
        INumberingRegistryRepository numberingRegistryRepository,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
        _numberingRegistryRepository = numberingRegistryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentResponse>> Handle(
        RegisterDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentNotFound);

        // Checked before allocating a sequence so a rejected registration never burns a number.
        if (document.CurrentStatusId != DocumentStatusType.DraftId)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.OnlyDraftRegisterable);

        if (document.ActiveOfficialNumberId is not null)
            return Result.Failure<DocumentResponse>(DomainErrors.Numbering.DocumentAlreadyNumbered);

        var numberingYear = request.NumberingYear
            ?? document.DocumentDate?.Year
            ?? DateTime.UtcNow.Year;

        var registryResult = await NumberingRegistryResolver.ResolveAsync(
            _numberingRegistryRepository,
            document.OwningDepartmentId,
            document.DocumentTypeId,
            document.DirectionId,
            cancellationToken);

        if (registryResult.IsFailure)
            return Result.Failure<DocumentResponse>(registryResult.Error);

        var registry = registryResult.Value;

        var sequenceResult = registry.AllocateNextSequence(numberingYear);
        if (sequenceResult.IsFailure)
            return Result.Failure<DocumentResponse>(sequenceResult.Error);

        var numberText = OfficialNumberFormatter.Format(
            registry.FormatPattern, numberingYear, sequenceResult.Value);

        if (await _documentNumberRepository.ExistsByNumberAndYearAsync(
                numberText, numberingYear, cancellationToken))
        {
            return Result.Failure<DocumentResponse>(DomainErrors.Numbering.OfficialNumberTaken);
        }

        var numberResult = DocumentNumber.Issue(
            document.DocumentId,
            registry.RegistryId,
            numberingYear,
            sequenceResult.Value,
            numberText,
            request.UserId);

        if (numberResult.IsFailure)
            return Result.Failure<DocumentResponse>(numberResult.Error);

        var officialNumberValue = OfficialNumberValue.Create(numberText);
        if (officialNumberValue.IsFailure)
            return Result.Failure<DocumentResponse>(officialNumberValue.Error);

        var registerResult = document.Register(
            numberResult.Value.DocumentNumberId,
            officialNumberValue.Value,
            request.UserId);

        if (registerResult.IsFailure)
            return Result.Failure<DocumentResponse>(registerResult.Error);

        _documentNumberRepository.Add(numberResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentResponse.From(document);
    }
}
