using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

internal sealed class IssueOfficialNumberCommandHandler
    : ICommandHandler<IssueOfficialNumberCommand, DocumentNumberResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly INumberingRegistryRepository _numberingRegistryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public IssueOfficialNumberCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository,
        INumberingRegistryRepository numberingRegistryRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
        _numberingRegistryRepository = numberingRegistryRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<DocumentNumberResponse>> Handle(
        IssueOfficialNumberCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentNumberResponse>(
                DomainErrors.Correspondence.DocumentNotFound);

        if (document.ActiveOfficialNumberId is not null)
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.DocumentAlreadyNumbered);

        if (document.CurrentStatusId != DocumentStatusType.DraftId)
            return Result.Failure<DocumentNumberResponse>(
                DomainErrors.Correspondence.OnlyDraftRegisterable);

        var numberingYear = request.NumberingYear
            ?? document.DocumentDate?.Year
            ?? _clock.UtcNow.Year;

        var registryResult = await NumberingRegistryResolver.ResolveAsync(
            _numberingRegistryRepository,
            document.OwningDepartmentId,
            document.DocumentTypeId,
            document.DirectionId,
            cancellationToken,
            _unitOfWork.UtcNow);

        if (registryResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(registryResult.Error);

        var registry = registryResult.Value;

        var sequenceResult = registry.AllocateNextSequence(numberingYear, _unitOfWork.UtcNow);
        if (sequenceResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(sequenceResult.Error);

        var numberText = OfficialNumberFormatter.Format(
            registry.FormatPattern, numberingYear, sequenceResult.Value);

        if (await _documentNumberRepository.ExistsByNumberAndYearAsync(
                numberText, numberingYear, cancellationToken))
        {
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.OfficialNumberTaken);
        }

        var numberResult = DocumentNumber.Issue(
            document.DocumentId,
            registry.RegistryId,
            numberingYear,
            sequenceResult.Value,
            numberText,
            request.UserId,
            utcNow: _unitOfWork.UtcNow);

        if (numberResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(numberResult.Error);

        var officialNumberValue = OfficialNumberValue.Create(numberText);
        if (officialNumberValue.IsFailure)
            return Result.Failure<DocumentNumberResponse>(officialNumberValue.Error);

        var registerResult = document.Register(
            numberResult.Value.DocumentNumberId,
            officialNumberValue.Value,
            request.UserId,
            utcNow: _unitOfWork.UtcNow);

        if (registerResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(registerResult.Error);

        _documentNumberRepository.Add(numberResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentNumberResponse.From(numberResult.Value);
    }
}
