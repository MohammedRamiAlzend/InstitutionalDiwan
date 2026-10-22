using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Infrastructure.Authorization;

/// <summary>
/// Resolves document-level access by combining, in order:
/// <list type="number">
///   <item><b>Explicit grant</b> — an active, unexpired <see cref="DocumentAccessGrant"/> for the
///   caller whose level is at least the requested one.</item>
///   <item><b>Authorship</b> — the user who created the document keeps full access to it.</item>
///   <item><b>Organizational scope</b> — <see cref="IDepartmentScopeEvaluator"/> over the document's
///   owning department. Scope alone grants up to <c>DOWNLOAD</c>; <c>FORWARD</c> and <c>MANAGE</c>
///   additionally require an institution-wide scope, because forwarding and managing are
///   institution-level acts.</item>
///   <item><b>Confidentiality</b> — <c>SECRET</c> and <c>TOP_SECRET</c> documents are never reachable
///   through scope alone: they need an explicit grant (or authorship), regardless of the scope.</item>
/// </list>
/// Movement participation (Module 05) is not part of the rule yet — it will be added when document
/// movements exist.
/// </summary>
public sealed class DocumentAccessEvaluator : IDocumentAccessEvaluator
{
    private readonly IUserContext _userContext;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessGrantRepository _grantRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IClock _clock;

    public DocumentAccessEvaluator(
        IUserContext userContext,
        IDocumentRepository documentRepository,
        IDocumentAccessGrantRepository grantRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IClock clock)
    {
        _userContext = userContext;
        _documentRepository = documentRepository;
        _grantRepository = grantRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _clock = clock;
    }

    public async Task<bool> CanAccessDocumentAsync(
        Guid documentId,
        int requiredAccessLevelId,
        CancellationToken cancellationToken = default)
    {
        if (!_userContext.IsAuthenticated || documentId == Guid.Empty || requiredAccessLevelId <= 0)
            return false;

        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null)
            return false;

        var utcNow = _clock.UtcNow;
        var grantedLevel = await GetGrantedLevelAsync(documentId, utcNow, cancellationToken);
        if (grantedLevel >= requiredAccessLevelId)
            return true;

        if (document.CreatedByUserId == _userContext.UserId)
            return true;

        if (IsConfidentialRestricted(document.ConfidentialityLevelId))
            return false;

        if (!await _departmentScopeEvaluator.CanAccessDepartmentAsync(
                document.OwningDepartmentId, cancellationToken))
        {
            return false;
        }

        if (requiredAccessLevelId > AccessLevel.DownloadId)
            return IsInstitutionWideScope(_userContext.ScopeTypeId);

        return true;
    }

    private async Task<int> GetGrantedLevelAsync(
        Guid documentId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var grants = await _grantRepository.ListByDocumentAsync(documentId, cancellationToken);

        return grants
            .Where(grant => grant.UserId == _userContext.UserId && grant.IsCurrentlyValid(utcNow))
            .Select(grant => grant.AccessLevelId)
            .DefaultIfEmpty(AccessLevel.ViewId - 1)
            .Max();
    }

    private static bool IsConfidentialRestricted(int confidentialityLevelId) =>
        confidentialityLevelId is DocumentConfidentialityLevel.SecretId
            or DocumentConfidentialityLevel.TopSecretId;

    private static bool IsInstitutionWideScope(int scopeTypeId) =>
        scopeTypeId is UserScopeType.InstitutionId or UserScopeType.SystemId;
}
