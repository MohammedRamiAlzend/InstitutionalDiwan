using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>Deactivates a user account (soft delete); the account can no longer sign in.</summary>
public sealed record DeactivateUserCommand(Guid UserId) : ICommand<UserResponse>;
