using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Movements;
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
///   <item><b>Movement participation</b> — a department on the current, dispatched step of the
///   document's active movement (the sender or the receiver) may <c>VIEW</c> and <c>DOWNLOAD</c>
///   it, even when it is outside the owning department's scope. Forwarding and managing still
///   require an explicit higher grant or an institution-wide scope.</item>
/// </list>
/// </summary>
public sealed class DocumentAccessEvaluator : IDocumentAccessEvaluator
{
    private readonly IUserContext _userContext;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessGrantRepository _grantRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IClock _clock;

    public DocumentAccessEvaluator(
        IUserContext userContext,
        IDocumentRepository documentRepository,
        IDocumentAccessGrantRepository grantRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IDocumentMovementRepository movementRepository,
        IClock clock)
    {
        _userContext = userContext;
        _documentRepository = documentRepository;
        _grantRepository = grantRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _movementRepository = movementRepository;
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

        if (await _departmentScopeEvaluator.CanAccessDepartmentAsync(
                document.OwningDepartmentId, cancellationToken))
        {
            if (requiredAccessLevelId > AccessLevel.DownloadId)
                return IsInstitutionWideScope(_userContext.ScopeTypeId);

            return true;
        }

        // Movement participation is an operational access path: the department that currently has
        // the document in flight can read and download it, but cannot gain FORWARD/MANAGE rights.
        if (requiredAccessLevelId > AccessLevel.DownloadId)
            return false;

        return await IsCurrentMovementParticipantAsync(documentId, cancellationToken);
    }

    /// <summary>
    /// True when the caller's organizational scope covers either side of the current step of the
    /// document's active movement. A draft step (not yet dispatched) counts for nobody.
    /// </summary>
    private async Task<bool> IsCurrentMovementParticipantAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetActiveByDocumentIdAsync(
            documentId, cancellationToken);

        var currentStep = movement?.Steps
            .FirstOrDefault(step => step.StepNo == movement.CurrentStepNo);

        if (currentStep is null ||
            currentStep.StepStatusId is not (MovementStepStatus.SentId
                or MovementStepStatus.ReceivedId
                or MovementStepStatus.CompletedId))
        {
            return false;
        }

        return await _departmentScopeEvaluator.CanAccessDepartmentAsync(
                   currentStep.FromDepartmentId, cancellationToken) ||
               await _departmentScopeEvaluator.CanAccessDepartmentAsync(
                   currentStep.ToDepartmentId, cancellationToken);
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
