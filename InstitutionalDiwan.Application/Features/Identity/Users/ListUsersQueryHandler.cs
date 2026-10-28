using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

internal sealed class ListUsersQueryHandler : IQueryHandler<ListUsersQuery, PagedResult<UserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public ListUsersQueryHandler(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<Result<PagedResult<UserResponse>>> Handle(
        ListUsersQuery request,
        CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize, request.Search, request.SortBy, request.SortDescending);
        var users = await _userRepository.ListPagedAsync(page, cancellationToken);

        var responses = new List<UserResponse>(users.Items.Count);
        foreach (var user in users.Items)
            responses.Add(await UserResponseFactory.CreateAsync(user, _roleRepository, cancellationToken));

        return Result.Success(new PagedResult<UserResponse>(
            responses,
            page.NormalizedPage,
            page.NormalizedPageSize,
            users.TotalCount));
    }
}
