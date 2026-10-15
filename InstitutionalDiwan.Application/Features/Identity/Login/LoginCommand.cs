using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Login;

/// <summary>Authenticates a user by username and password and issues an access token.</summary>
public sealed record LoginCommand(string Username, string Password) : ICommand<LoginResponse>;
