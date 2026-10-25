using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class RedirectMovementCommandHandler
    : ICommandHandler<RedirectMovementCommand, MovementResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RedirectMovementCommandHandler(
        IDocumentMovementRepository movementRepository,
        IDepartmentRepository departmentRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _movementRepository = movementRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MovementResponse>> Handle(
        RedirectMovementCommand request,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return Result.Failure<MovementResponse>(DomainErrors.Movements.MovementNotFound);

        var destination = await _departmentRepository.GetByIdAsync(
            request.NewDestinationDepartmentId, cancellationToken);
        if (destination is null)
            return Result.Failure<MovementResponse>(DomainErrors.Department.NotFound);

        if (!destination.IsActive)
            return Result.Failure<MovementResponse>(DomainErrors.Department.Inactive);

        if (request.AssignedToUserId is { } assignedToUserId)
        {
            var assignee = await _userRepository.GetByIdAsync(assignedToUserId, cancellationToken);
            if (assignee is null)
                return Result.Failure<MovementResponse>(DomainErrors.Identity.UserNotFound);
        }

        var redirect = movement.RedirectTo(
            destination.DepartmentId,
            request.UserId,
            request.AssignedToUserId,
            request.Instructions,
            request.DueDate,
            _unitOfWork.UtcNow);
        if (redirect.IsFailure)
            return Result.Failure<MovementResponse>(redirect.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MovementResponse.From(movement);
    }
}
