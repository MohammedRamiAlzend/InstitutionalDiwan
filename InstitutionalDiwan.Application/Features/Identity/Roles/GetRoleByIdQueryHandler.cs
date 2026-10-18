using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

internal sealed class GetRoleByIdQueryHandler : IQueryHandler<GetRoleByIdQuery, RoleResponse>
{
    private readonly IRoleRepository _roleRepository;

    public GetRoleByIdQueryHandler(IRoleRepository roleRepository) => _roleRepository = roleRepository;

    public async Task<Result<RoleResponse>> Handle(
        GetRoleByIdQuery request,
        CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure<RoleResponse>(DomainErrors.Identity.RoleNotFound);

        return RoleResponse.From(role);
    }
}
