using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.AccessGrants;

/// <summary>Revokes a previously granted document access grant.</summary>
public sealed record RevokeDocumentAccessCommand(Guid GrantId, Guid RevokedByUserId)
    : ICommand<DocumentAccessGrantResponse>;
