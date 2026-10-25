using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class CreateMovementCommandHandler
    : ICommandHandler<CreateMovementCommand, MovementResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly CentralDiwanRouting _centralDiwanRouting;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMovementCommandHandler(
        IDocumentMovementRepository movementRepository,
        IDocumentRepository documentRepository,
        IDepartmentRepository departmentRepository,
        IUserRepository userRepository,
        CentralDiwanRouting centralDiwanRouting,
        IUnitOfWork unitOfWork)
    {
        _movementRepository = movementRepository;
        _documentRepository = documentRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _centralDiwanRouting = centralDiwanRouting;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MovementResponse>> Handle(
        CreateMovementCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<MovementResponse>(DomainErrors.Identity.UserNotFound);

        if (!user.IsActive)
            return Result.Failure<MovementResponse>(DomainErrors.Identity.UserInactive);

        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<MovementResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (await _movementRepository.HasActiveMovementsAsync(document.DocumentId, cancellationToken))
            return Result.Failure<MovementResponse>(DomainErrors.Movements.ActiveMovementExists);

        if (document.CurrentStatusId != DocumentStatusType.RegisteredId)
            return Result.Failure<MovementResponse>(DomainErrors.Correspondence.OnlyRegisteredRoutable);

        var destination = await _departmentRepository.GetByIdAsync(
            request.DestinationDepartmentId, cancellationToken);
        if (destination is null)
            return Result.Failure<MovementResponse>(DomainErrors.Department.NotFound);

        if (!destination.IsActive)
            return Result.Failure<MovementResponse>(DomainErrors.Department.Inactive);

        var routeResult = await _centralDiwanRouting.ValidateRouteAsync(
            user.PrimaryDepartmentId, destination.DepartmentId, cancellationToken);
        if (routeResult.IsFailure)
            return Result.Failure<MovementResponse>(routeResult.Error);

        if (request.AssignedToUserId is { } assignedToUserId)
        {
            var assignee = await _userRepository.GetByIdAsync(assignedToUserId, cancellationToken);
            if (assignee is null)
                return Result.Failure<MovementResponse>(DomainErrors.Identity.UserNotFound);
        }

        var movementResult = DocumentMovement.Create(
            document.DocumentId,
            user.PrimaryDepartmentId,
            destination.DepartmentId,
            user.UserId,
            request.MovementTypeId,
            request.AssignedToUserId,
            request.Instructions,
            request.DueDate,
            utcNow: _unitOfWork.UtcNow);

        if (movementResult.IsFailure)
            return Result.Failure<MovementResponse>(movementResult.Error);

        var movement = movementResult.Value;

        var inMovement = document.MarkInMovement(
            user.UserId, $"Routed to {destination.NameAr}", _unitOfWork.UtcNow);
        if (inMovement.IsFailure)
            return Result.Failure<MovementResponse>(inMovement.Error);

        _movementRepository.Add(movement);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MovementResponse.From(movement);
    }
}
