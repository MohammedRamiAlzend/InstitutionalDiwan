using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>Fetches a single user account, including its role codes, by id.</summary>
public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserResponse>;
