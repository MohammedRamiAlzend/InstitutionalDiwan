using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>Reactivates a previously deactivated user account.</summary>
public sealed record ActivateUserCommand(Guid UserId) : ICommand<UserResponse>;
