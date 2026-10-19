using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class CreateDraftDocumentCommandHandler
    : ICommandHandler<CreateDraftDocumentCommand, DocumentResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDraftDocumentCommandHandler(
        IUserRepository userRepository,
        IDocumentRepository documentRepository,
        IDocumentTypeRepository documentTypeRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _documentRepository = documentRepository;
        _documentTypeRepository = documentTypeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentResponse>> Handle(
        CreateDraftDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<DocumentResponse>(DomainErrors.Identity.UserNotFound);

        if (!user.IsActive)
            return Result.Failure<DocumentResponse>(DomainErrors.Identity.UserInactive);

        var documentType = await _documentTypeRepository.GetByIdAsync(
            request.DocumentTypeId, cancellationToken);
        if (documentType is null)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentTypeNotFound);

        if (!documentType.IsActive)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentTypeInactive);

        var documentResult = Document.CreateDraft(
            request.Title,
            request.DocumentTypeId,
            request.DirectionId,
            user.PrimaryDepartmentId,
            user.UserId,
            request.SubjectSummary,
            request.ConfidentialityLevelId ?? DocumentConfidentialityLevel.NormalId,
            request.PriorityLevelId ?? DocumentPriorityLevel.NormalId,
            request.DocumentDate,
            request.ExternalReferenceNumber,
            request.ExternalDocumentDate);

        if (documentResult.IsFailure)
            return Result.Failure<DocumentResponse>(documentResult.Error);

        _documentRepository.Add(documentResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentResponse.From(documentResult.Value);
    }
}
