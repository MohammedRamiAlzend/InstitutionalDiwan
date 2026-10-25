using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Application.Features.Identity.Login;

internal sealed class GetUserProfileQueryHandler
    : IQueryHandler<GetUserProfileQuery, UserProfileResponse>
{
    private readonly IUserContext _userContext;

    public GetUserProfileQueryHandler(IUserContext userContext) => _userContext = userContext;

    public Task<Result<UserProfileResponse>> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        // The endpoint is authorized, so this only fires when a handler is invoked outside an
        // authenticated request (for example from a future background job).
        if (!_userContext.IsAuthenticated)
            return Task.FromResult(
                Result.Failure<UserProfileResponse>(DomainErrors.Identity.NotAuthenticated));

        // Ordered so the response is stable regardless of claim order in the token.
        var roles = _userContext.Roles
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToList();

        var permissions = _userContext.Permissions
            .OrderBy(permission => permission, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(Result.Success(new UserProfileResponse(
            _userContext.UserId,
            _userContext.Username,
            roles,
            permissions)));
    }
}
