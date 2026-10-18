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
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly INumberingRegistryRepository _numberingRegistryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDocumentCommandHandler(
        IUserRepository userRepository,
        IDocumentRepository documentRepository,
        IDocumentTypeRepository documentTypeRepository,
        IDocumentNumberRepository documentNumberRepository,
        INumberingRegistryRepository numberingRegistryRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _documentRepository = documentRepository;
        _documentTypeRepository = documentTypeRepository;
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

        var documentType = await _documentTypeRepository.GetByIdAsync(request.DocumentTypeId, cancellationToken);
        if (documentType is null)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentTypeNotFound);

        if (!documentType.IsActive)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentTypeInactive);

        var numberText = (request.OfficialNumberText ?? string.Empty).Trim();
        if (numberText.Length == 0)
            return Result.Failure<DocumentResponse>(DomainErrors.Numbering.OfficialNumberEmpty);

        if (await _documentNumberRepository.ExistsByNumberAndYearAsync(
                numberText, request.NumberingYear, cancellationToken))
            return Result.Failure<DocumentResponse>(DomainErrors.Numbering.OfficialNumberTaken);

        var documentResult = Document.CreateDraft(
            title: request.Title,
            documentTypeId: request.DocumentTypeId,
            directionId: request.DirectionId,
            owningDepartmentId: user.PrimaryDepartmentId,
            createdByUserId: user.UserId,
            subjectSummary: request.SubjectSummary,
            confidentialityLevelId: request.ConfidentialityLevelId ?? DocumentConfidentialityLevel.NormalId,
            priorityLevelId: request.PriorityLevelId ?? DocumentPriorityLevel.NormalId);

        if (documentResult.IsFailure)
            return Result.Failure<DocumentResponse>(documentResult.Error);

        var document = documentResult.Value;

        var registryResult = await ResolveRegistryAsync(
            user.PrimaryDepartmentId, request.DocumentTypeId, request.DirectionId, cancellationToken);
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
        int documentTypeId,
        int directionId,
        CancellationToken cancellationToken)
    {
        var registry = await _numberingRegistryRepository.GetActiveForScopeAsync(
            departmentId, documentTypeId, directionId, cancellationToken);

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
