using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Login;

internal sealed class GetUserProfileQueryHandler
    : IQueryHandler<GetUserProfileQuery, UserProfileResponse>
{
    private readonly IUserContext _userContext;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IInstitutionSettingRepository _settingRepository;

    public GetUserProfileQueryHandler(
        IUserContext userContext,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IInstitutionSettingRepository settingRepository)
    {
        _userContext = userContext;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _settingRepository = settingRepository;
    }

    public async Task<Result<UserProfileResponse>> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        // The endpoint is authorized, so this only fires when a handler is invoked outside an
        // authenticated request (for example from a future background job).
        if (!_userContext.IsAuthenticated)
            return Result.Failure<UserProfileResponse>(DomainErrors.Identity.NotAuthenticated);

        // Ordered so the response is stable regardless of claim order in the token.
        var roles = _userContext.Roles
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToList();

        var permissions = _userContext.Permissions
            .OrderBy(permission => permission, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var accessibleDepartmentIds = await _departmentScopeEvaluator
            .GetAccessibleDepartmentIdsAsync(cancellationToken);

        var setting = await _settingRepository.GetSingletonAsync(cancellationToken);
        var canCreateDocuments = setting is null ||
            setting.CanCreateDocuments(_userContext.PrimaryDepartmentId);

        return Result.Success(new UserProfileResponse(
            _userContext.UserId,
            _userContext.Username,
            roles,
            permissions,
            accessibleDepartmentIds?.OrderBy(departmentId => departmentId).ToList(),
            _userContext.PrimaryDepartmentId,
            canCreateDocuments));
    }
}
