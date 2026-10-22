using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>A page of user accounts with their roles, optionally filtered by a username search.</summary>
public sealed record ListUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IQuery<PagedResult<UserResponse>>;
