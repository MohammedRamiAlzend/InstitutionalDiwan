using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Entities.Identity;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>
/// Creates an administrative user account. The raw password is hashed by the handler; roles are
/// assigned separately.
/// </summary>
public sealed record CreateUserCommand(
    string Username,
    string Password,
    Guid PrimaryDepartmentId,
    Guid? EmployeeId = null,
    int ScopeTypeId = UserScopeType.DepartmentId,
    bool MustChangePassword = true) : ICommand<UserResponse>;
