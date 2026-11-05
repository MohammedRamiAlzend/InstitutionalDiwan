using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

internal sealed class ReplaceOfficialNumberCommandHandler
    : ICommandHandler<ReplaceOfficialNumberCommand, DocumentNumberResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly INumberingRegistryRepository _numberingRegistryRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReplaceOfficialNumberCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository,
        INumberingRegistryRepository numberingRegistryRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
        _numberingRegistryRepository = numberingRegistryRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<DocumentNumberResponse>> Handle(
        ReplaceOfficialNumberCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentNumberResponse>(
                DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.DownloadId, cancellationToken))
        {
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        // Checked here as well as in the domain so a rejected call never consumes a registry number.
        var notArchived = document.EnsureNotArchived();
        if (notArchived.IsFailure)
            return Result.Failure<DocumentNumberResponse>(notArchived.Error);

        var replaced = await _documentNumberRepository.GetByIdAsync(
            request.DocumentNumberId, cancellationToken);

        if (replaced is null || replaced.DocumentId != document.DocumentId)
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.NumberNotFound);

        if (replaced.StatusId != DocumentNumberStatus.ActiveId)
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.NumberNotActive);

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

        var numberingYear = request.NumberingYear
            ?? document.DocumentDate?.Year
            ?? _clock.UtcNow.Year;

        var sequenceResult = registry.AllocateNextSequence(numberingYear, _unitOfWork.UtcNow);
        if (sequenceResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(sequenceResult.Error);

        var numberText = OfficialNumberFormatter.NewSystemNumber();

        if (await _documentNumberRepository.ExistsByNumberAndYearAsync(
                numberText, numberingYear, cancellationToken))
        {
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.OfficialNumberTaken);
        }

        var replacementResult = DocumentNumber.Issue(
            document.DocumentId,
            registry.RegistryId,
            numberingYear,
            sequenceResult.Value,
            numberText,
            request.UserId,
            $"Replaces {replaced.OfficialNumberText}",
            utcNow: _unitOfWork.UtcNow);

        if (replacementResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(replacementResult.Error);

        var replaceResult = replaced.ReplaceWith(
            replacementResult.Value.DocumentNumberId, request.UserId, request.Reason, _unitOfWork.UtcNow);

        if (replaceResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(replaceResult.Error);

        var officialNumberValue = OfficialNumberValue.Create(numberText);
        if (officialNumberValue.IsFailure)
            return Result.Failure<DocumentNumberResponse>(officialNumberValue.Error);

        var attachResult = document.AttachReplacementNumber(
            replacementResult.Value.DocumentNumberId, officialNumberValue.Value, _unitOfWork.UtcNow);

        if (attachResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(attachResult.Error);

        _documentNumberRepository.Add(replacementResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentNumberResponse.From(replacementResult.Value);
    }
}
