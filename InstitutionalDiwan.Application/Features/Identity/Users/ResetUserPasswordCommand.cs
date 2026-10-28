using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>
/// Resets a user's forgotten password on an administrator's behalf. The owner never typed the new
/// password, so the account is flagged to change it at the next sign-in; <c>ResetByUserId</c> is the
/// administrator the endpoint derived from the token, and is recorded for the audit chain.
/// </summary>
public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword, Guid? ResetByUserId = null)
    : ICommand<UserResponse>;
