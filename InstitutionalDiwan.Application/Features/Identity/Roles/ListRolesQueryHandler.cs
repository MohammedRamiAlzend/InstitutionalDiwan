using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

internal sealed class ListRolesQueryHandler
    : IQueryHandler<ListRolesQuery, IReadOnlyCollection<RoleResponse>>
{
    private readonly IRoleRepository _roleRepository;

    public ListRolesQueryHandler(IRoleRepository roleRepository) => _roleRepository = roleRepository;

    public async Task<Result<IReadOnlyCollection<RoleResponse>>> Handle(
        ListRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.ListAllAsync(cancellationToken);
        IReadOnlyCollection<RoleResponse> responses = roles.Select(RoleResponse.From).ToList();
        return Result.Success(responses);
    }
}
