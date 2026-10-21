using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Lists the documents a department sent out and that are still with the receiving side — the
/// movements whose current step leaves that department and has not come back yet.
/// </summary>
/// <param name="UserId">Caller id, used to resolve the department when none is given.</param>
/// <param name="DepartmentId">
/// Department to read. When omitted the caller's own primary department is used; when given it
/// must be inside the caller's organizational scope.
/// </param>
public sealed record GetDepartmentOutboxQuery(Guid UserId, Guid? DepartmentId = null)
    : IQuery<IReadOnlyCollection<MovementQueueItemResponse>>;
