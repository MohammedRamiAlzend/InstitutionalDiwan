using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Register;

/// <summary>Creates a new self-service user account with the default USER role.</summary>
public sealed record RegisterCommand(string Username, string Password) : ICommand<RegisterResponse>;
