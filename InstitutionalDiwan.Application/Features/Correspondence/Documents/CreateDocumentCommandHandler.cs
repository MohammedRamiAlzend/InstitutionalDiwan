using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class CreateDocumentCommandHandler : ICommandHandler<CreateDocumentCommand, DocumentResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDocumentCommandHandler(
        IUserRepository userRepository,
        IDocumentRepository documentRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _documentRepository = documentRepository;
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

        _documentRepository.Add(documentResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentResponse.From(documentResult.Value);
    }
}
