using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class GetDepartmentInboxQueryHandler
    : IQueryHandler<GetDepartmentInboxQuery, IReadOnlyCollection<MovementQueueItemResponse>>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IClock _clock;

    public GetDepartmentInboxQueryHandler(
        IDocumentMovementRepository movementRepository,
        IDocumentRepository documentRepository,
        IUserRepository userRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IClock clock)
    {
        _clock = clock;
        _movementRepository = movementRepository;
        _documentRepository = documentRepository;
        _userRepository = userRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
    }

    public async Task<Result<IReadOnlyCollection<MovementQueueItemResponse>>> Handle(
        GetDepartmentInboxQuery request,
        CancellationToken cancellationToken)
    {
        var departmentId = await MovementQueue.ResolveDepartmentIdAsync(
            request.UserId,
            request.DepartmentId,
            _userRepository,
            _departmentScopeEvaluator,
            cancellationToken);

        if (departmentId.IsFailure)
            return Result.Failure<IReadOnlyCollection<MovementQueueItemResponse>>(departmentId.Error);

        var movements = await _movementRepository.ListInboxAsync(
            departmentId.Value, cancellationToken);

        return Result.Success<IReadOnlyCollection<MovementQueueItemResponse>>(
            await MovementQueue.BuildAsync(
                movements, _documentRepository, _departmentScopeEvaluator, request.UserId, _clock, cancellationToken));
    }
}
