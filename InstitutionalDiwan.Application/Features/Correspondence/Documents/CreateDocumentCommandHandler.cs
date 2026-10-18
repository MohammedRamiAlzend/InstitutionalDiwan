using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class CreateDocumentCommandHandler : ICommandHandler<CreateDocumentCommand, DocumentResponse>
{
    private const string DefaultRegistryCode = "DEFAULT";

    private readonly IUserRepository _userRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly INumberingRegistryRepository _numberingRegistryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDocumentCommandHandler(
        IUserRepository userRepository,
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository,
        INumberingRegistryRepository numberingRegistryRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
        _numberingRegistryRepository = numberingRegistryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentResponse>> Handle(
        CreateDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<DocumentResponse>(DomainErrors.Identity.UserNotFound);

        if (!user.IsActive)
            return Result.Failure<DocumentResponse>(DomainErrors.Identity.UserInactive);

        var numberText = (request.OfficialNumberText ?? string.Empty).Trim();
        if (numberText.Length == 0)
            return Result.Failure<DocumentResponse>(DomainErrors.Numbering.OfficialNumberEmpty);

        if (await _documentNumberRepository.ExistsByNumberAndYearAsync(
                numberText, request.NumberingYear, cancellationToken))
            return Result.Failure<DocumentResponse>(DomainErrors.Numbering.OfficialNumberTaken);

        var documentResult = Document.CreateDraft(
            title: request.Title,
            documentTypeCode: request.DocumentTypeCode,
            direction: request.Direction,
            owningDepartmentId: user.PrimaryDepartmentId,
            createdByUserId: user.UserId,
            subjectSummary: request.SubjectSummary,
            confidentialityLevel: request.ConfidentialityLevel ?? "NORMAL",
            priorityLevel: request.PriorityLevel ?? "NORMAL");

        if (documentResult.IsFailure)
            return Result.Failure<DocumentResponse>(documentResult.Error);

        var document = documentResult.Value;

        var registryResult = await ResolveRegistryAsync(
            user.PrimaryDepartmentId, request.DocumentTypeCode, request.Direction, cancellationToken);
        if (registryResult.IsFailure)
            return Result.Failure<DocumentResponse>(registryResult.Error);

        var registry = registryResult.Value;

        var sequenceResult = registry.AllocateNextSequence(request.NumberingYear);
        if (sequenceResult.IsFailure)
            return Result.Failure<DocumentResponse>(sequenceResult.Error);

        var numberResult = DocumentNumber.Issue(
            document.DocumentId,
            registry.RegistryId,
            request.NumberingYear,
            sequenceResult.Value,
            numberText,
            user.UserId);
        if (numberResult.IsFailure)
            return Result.Failure<DocumentResponse>(numberResult.Error);

        var officialNumberValue = OfficialNumberValue.Create(numberText);
        if (officialNumberValue.IsFailure)
            return Result.Failure<DocumentResponse>(officialNumberValue.Error);

        var registerResult = document.Register(
            numberResult.Value.DocumentNumberId,
            officialNumberValue.Value,
            user.UserId);
        if (registerResult.IsFailure)
            return Result.Failure<DocumentResponse>(registerResult.Error);

        _documentRepository.Add(document);
        _documentNumberRepository.Add(numberResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentResponse.From(document);
    }

    private async Task<Result<NumberingRegistry>> ResolveRegistryAsync(
        Guid departmentId,
        string? documentTypeCode,
        string? direction,
        CancellationToken cancellationToken)
    {
        var scopeDocumentType = string.IsNullOrWhiteSpace(documentTypeCode)
            ? null
            : documentTypeCode.Trim().ToUpperInvariant();
        var scopeDirection = string.IsNullOrWhiteSpace(direction)
            ? null
            : direction.Trim().ToUpperInvariant();

        var registry = await _numberingRegistryRepository.GetActiveForScopeAsync(
            departmentId, scopeDocumentType, scopeDirection, cancellationToken);

        if (registry is not null)
            return Result.Success(registry);

        var created = NumberingRegistry.Create(
            DefaultRegistryCode,
            "سجل الترقيم الافتراضي");
        if (created.IsFailure)
            return Result.Failure<NumberingRegistry>(created.Error);

        _numberingRegistryRepository.Add(created.Value);
        return Result.Success(created.Value);
    }
}
