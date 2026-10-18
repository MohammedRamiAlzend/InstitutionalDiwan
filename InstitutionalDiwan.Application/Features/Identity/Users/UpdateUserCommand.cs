using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>Updates a user's primary department, employee link, scope, and password-change flag.</summary>
public sealed record UpdateUserCommand(
    Guid UserId,
    Guid PrimaryDepartmentId,
    Guid? EmployeeId,
    int ScopeTypeId,
    bool MustChangePassword) : ICommand<UserResponse>;
