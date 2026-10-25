using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Login;

/// <summary>
/// Reads the caller's profile from the identity attached to the current request. The query carries
/// no state because the caller is ambient: the handler reads <c>IUserContext</c>, which the host
/// projects from the signed-in token.
/// </summary>
public sealed record GetUserProfileQuery : IQuery<UserProfileResponse>;
