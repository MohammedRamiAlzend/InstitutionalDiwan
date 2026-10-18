using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

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
            responses.Add(await UserResponseFactory.CreateAsync(user, _roleRepository, cancellationToken));

        return Result.Success<IReadOnlyCollection<UserResponse>>(responses);
    }
}
