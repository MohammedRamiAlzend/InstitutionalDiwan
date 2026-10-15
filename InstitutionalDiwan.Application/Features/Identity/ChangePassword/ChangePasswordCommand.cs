using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Identity.Login;

namespace InstitutionalDiwan.Application.Features.Identity.ChangePassword;

/// <summary>
/// Changes the authenticated user's password (identified by <c>UserId</c>, which the endpoint
/// derives from the token — never from the request body) and re-issues their access token.
/// </summary>
public sealed record ChangePasswordCommand(Guid UserId, string OldPassword, string NewPassword)
    : ICommand<LoginResponse>;
