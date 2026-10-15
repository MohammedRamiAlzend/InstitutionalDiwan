using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.ListUsers;

internal sealed class ListUsersQueryHandler : IQueryHandler<ListUsersQuery, IReadOnlyCollection<UserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public ListUsersQueryHandler(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<Result<IReadOnlyCollection<UserResponse>>> Handle(
        ListUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.ListAllAsync(cancellationToken);
        var responses = new List<UserResponse>(users.Count);

        foreach (var user in users)
        {
            var roleCodes = new List<string>();
            foreach (var userRole in user.UserRoles)
            {
                var role = await _roleRepository.GetByIdAsync(userRole.RoleId, cancellationToken);
                if (role is not null)
                    roleCodes.Add(role.Code);
            }

            responses.Add(new UserResponse(
                user.UserId,
                user.Username,
                user.IsActive,
                user.MustChangePassword,
                user.LastLoginAt,
                roleCodes));
        }

        return Result.Success<IReadOnlyCollection<UserResponse>>(responses);
    }
}
