using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.AccessGrants;

/// <summary>Revokes a grant only when it belongs to the document named by the route.</summary>
public sealed record RevokeDocumentAccessCommand(Guid DocumentId, Guid GrantId, Guid RevokedByUserId)
    : ICommand<DocumentAccessGrantResponse>;
