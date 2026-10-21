using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Lists the documents waiting in a department's inbox — the movements whose current step is
/// addressed to that department and still awaits the department's action.
/// </summary>
/// <param name="UserId">Caller id, used to resolve the department when none is given.</param>
/// <param name="DepartmentId">
/// Department to read. When omitted the caller's own primary department is used; when given it
/// must be inside the caller's organizational scope.
/// </param>
public sealed record GetDepartmentInboxQuery(Guid UserId, Guid? DepartmentId = null)
    : IQuery<IReadOnlyCollection<MovementQueueItemResponse>>;
