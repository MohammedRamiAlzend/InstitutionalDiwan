using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.AccessGrants;

/// <summary>Grants a user an access level on a document.</summary>
public sealed record GrantDocumentAccessCommand(
    Guid DocumentId,
    Guid UserId,
    int AccessLevelId,
    Guid GrantedByUserId,
    string? Reason = null,
    DateTime? ExpiresAt = null) : ICommand<DocumentAccessGrantResponse>;
