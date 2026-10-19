using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>
/// Ends a temporal department assignment. The assignment row is retained with its end date so the
/// user's historical scope stays auditable.
/// </summary>
public sealed record EndUserDepartmentScopeCommand(
    Guid UserId,
    Guid AssignmentId) : ICommand<UserResponse>;
