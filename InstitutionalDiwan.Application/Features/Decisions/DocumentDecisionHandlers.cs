using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Application.Abstractions.Time;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Archiving;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Decisions;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Decisions;

internal sealed class IssueDocumentDecisionCommandHandler
    : ICommandHandler<IssueDocumentDecisionCommand, DocumentDecisionResponse>
{
    private readonly IDocumentRepository _documents;
    private readonly IDecisionTypeRepository _types;
    private readonly IDocumentDecisionRepository _decisions;
    private readonly IDocumentFileRepository _files;
    private readonly IDepartmentScopeEvaluator _scope;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationPublisher _notifications;

    public IssueDocumentDecisionCommandHandler(
        IDocumentRepository documents,
        IDecisionTypeRepository types,
        IDocumentDecisionRepository decisions,
        IDocumentFileRepository files,
        IDepartmentScopeEvaluator scope,
        IUnitOfWork unitOfWork,
        INotificationPublisher notifications)
    {
        _documents = documents;
        _types = types;
        _decisions = decisions;
        _files = files;
        _scope = scope;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
    }

    public async Task<Result<DocumentDecisionResponse>> Handle(
        IssueDocumentDecisionCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documents.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (document.CurrentStatusId == DocumentStatusType.ArchivedId)
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Correspondence.DocumentArchived);

        if (document.CurrentStatusId == DocumentStatusType.CanceledId)
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Correspondence.AlreadyCanceled);

        var visit = await _decisions.FindOpenVisitAsync(document.DocumentId, cancellationToken);
        if (visit is null || !await _scope.CanAccessDepartmentAsync(visit.DepartmentId, cancellationToken))
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Decisions.NotHoldingDepartment);

        var type = await _types.GetByIdAsync(request.DecisionTypeId, cancellationToken);
        if (type is null)
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Decisions.TypeNotFound);

        if (request.DocumentFileId is { } fileId)
        {
            var file = await DecisionFileChecks.EnsureAsync(_files, document.DocumentId, fileId, cancellationToken);
            if (file.IsFailure)
                return Result.Failure<DocumentDecisionResponse>(file.Error);
        }

        var active = await _decisions.GetActiveByDocumentAsync(document.DocumentId, cancellationToken);
        var numberTaken = await _decisions.IsNumberTakenAsync(
            visit.DepartmentId, request.DecisionDate.Year, request.DecisionNumber.Trim(), cancellationToken);

        var issued = DocumentDecision.Issue(
            document.DocumentId,
            document.DocumentTypeId,
            type,
            request.DecisionNumber,
            request.DecisionDate,
            visit.DepartmentId,
            visit.VisitId,
            request.Summary,
            request.DocumentFileId,
            request.UserId,
            activeDecisionExists: active is not null,
            numberTaken,
            _unitOfWork.UtcNow);
        if (issued.IsFailure)
            return Result.Failure<DocumentDecisionResponse>(issued.Error);

        _decisions.Add(issued.Value);
        await _notifications.PublishToDepartmentAsync(
            NotificationType.DecisionIssuedId, document, document.OwningDepartmentId, request.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DecisionMapping.ToResponse(issued.Value, type.NameAr));
    }
}

internal sealed class AttachDecisionFileCommandHandler
    : ICommandHandler<AttachDecisionFileCommand, DocumentDecisionResponse>
{
    private readonly IDocumentDecisionRepository _decisions;
    private readonly IDecisionTypeRepository _types;
    private readonly IDocumentFileRepository _files;
    private readonly IDepartmentScopeEvaluator _scope;
    private readonly IUnitOfWork _unitOfWork;

    public AttachDecisionFileCommandHandler(
        IDocumentDecisionRepository decisions,
        IDecisionTypeRepository types,
        IDocumentFileRepository files,
        IDepartmentScopeEvaluator scope,
        IUnitOfWork unitOfWork)
    {
        _decisions = decisions;
        _types = types;
        _files = files;
        _scope = scope;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentDecisionResponse>> Handle(
        AttachDecisionFileCommand request,
        CancellationToken cancellationToken)
    {
        var decision = await _decisions.GetByIdAsync(request.DecisionId, cancellationToken);
        if (decision is null)
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Decisions.DecisionNotFound);

        var visit = await _decisions.FindOpenVisitAsync(decision.DocumentId, cancellationToken);
        if (visit is null || !await _scope.CanAccessDepartmentAsync(visit.DepartmentId, cancellationToken))
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Decisions.NotHoldingDepartment);

        var file = await DecisionFileChecks.EnsureAsync(
            _files, decision.DocumentId, request.DocumentFileId, cancellationToken);
        if (file.IsFailure)
            return Result.Failure<DocumentDecisionResponse>(file.Error);

        var attached = decision.AttachFile(request.DocumentFileId, _unitOfWork.UtcNow);
        if (attached.IsFailure)
            return Result.Failure<DocumentDecisionResponse>(attached.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var type = await _types.GetByIdAsync(decision.DecisionTypeId, cancellationToken);
        return Result.Success(DecisionMapping.ToResponse(decision, type?.NameAr ?? string.Empty));
    }
}

internal sealed class CancelDecisionCommandHandler
    : ICommandHandler<CancelDecisionCommand, DocumentDecisionResponse>
{
    private readonly IDocumentDecisionRepository _decisions;
    private readonly IDecisionTypeRepository _types;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public CancelDecisionCommandHandler(
        IDocumentDecisionRepository decisions,
        IDecisionTypeRepository types,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _decisions = decisions;
        _types = types;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentDecisionResponse>> Handle(
        CancelDecisionCommand request,
        CancellationToken cancellationToken)
    {
        if (_userContext.ScopeTypeId != UserScopeType.SystemId)
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Decisions.NotSystemAdmin);

        var decision = await _decisions.GetByIdAsync(request.DecisionId, cancellationToken);
        if (decision is null)
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Decisions.DecisionNotFound);

        var canceled = decision.Cancel(request.UserId, request.Reason, _unitOfWork.UtcNow);
        if (canceled.IsFailure)
            return Result.Failure<DocumentDecisionResponse>(canceled.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var type = await _types.GetByIdAsync(decision.DecisionTypeId, cancellationToken);
        return Result.Success(DecisionMapping.ToResponse(decision, type?.NameAr ?? string.Empty));
    }
}

internal sealed class RecordDecisionAppealCommandHandler
    : ICommandHandler<RecordDecisionAppealCommand, DocumentDecisionResponse>
{
    private readonly IDocumentRepository _documents;
    private readonly IDocumentDecisionRepository _decisions;
    private readonly IDecisionTypeRepository _types;
    private readonly IDepartmentRepository _departments;
    private readonly IDepartmentScopeEvaluator _scope;
    private readonly IUnitOfWork _unitOfWork;

    public RecordDecisionAppealCommandHandler(
        IDocumentRepository documents,
        IDocumentDecisionRepository decisions,
        IDecisionTypeRepository types,
        IDepartmentRepository departments,
        IDepartmentScopeEvaluator scope,
        IUnitOfWork unitOfWork)
    {
        _documents = documents;
        _decisions = decisions;
        _types = types;
        _departments = departments;
        _scope = scope;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentDecisionResponse>> Handle(
        RecordDecisionAppealCommand request,
        CancellationToken cancellationToken)
    {
        var decision = await _decisions.GetByIdAsync(request.DecisionId, cancellationToken);
        if (decision is null)
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Decisions.DecisionNotFound);

        var document = await _documents.GetByIdAsync(decision.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Correspondence.DocumentNotFound);

        // The appeal changes the decision only. An archived document stays archived, and neither
        // an open register visit nor custody of the file is required.
        var appellantDepartmentId = request.AppellantDepartmentId is { } requested && requested != Guid.Empty
            ? requested
            : document.OwningDepartmentId;

        if (request.AppellantDepartmentId is { } explicitId && explicitId != Guid.Empty &&
            await _departments.GetByIdAsync(explicitId, cancellationToken) is null)
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Department.NotFound);

        if (!await AppealScope.CoversAsync(_scope, decision.IssuingDepartmentId, appellantDepartmentId, cancellationToken))
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Decisions.AppealOutOfScope);

        var recorded = decision.RecordAppeal(
            request.AppealDate,
            appellantDepartmentId,
            request.ReferenceNumber,
            request.Notes,
            request.UserId,
            _unitOfWork.UtcNow);
        if (recorded.IsFailure)
            return Result.Failure<DocumentDecisionResponse>(recorded.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var type = await _types.GetByIdAsync(decision.DecisionTypeId, cancellationToken);
        return Result.Success(DecisionMapping.ToResponse(decision, type?.NameAr ?? string.Empty));
    }
}

internal sealed class RecordAppealOutcomeCommandHandler
    : ICommandHandler<RecordAppealOutcomeCommand, DocumentDecisionResponse>
{
    private readonly IDocumentDecisionRepository _decisions;
    private readonly IDecisionTypeRepository _types;
    private readonly IDepartmentScopeEvaluator _scope;
    private readonly IUnitOfWork _unitOfWork;

    public RecordAppealOutcomeCommandHandler(
        IDocumentDecisionRepository decisions,
        IDecisionTypeRepository types,
        IDepartmentScopeEvaluator scope,
        IUnitOfWork unitOfWork)
    {
        _decisions = decisions;
        _types = types;
        _scope = scope;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentDecisionResponse>> Handle(
        RecordAppealOutcomeCommand request,
        CancellationToken cancellationToken)
    {
        var decision = await _decisions.GetByIdAsync(request.DecisionId, cancellationToken);
        if (decision is null)
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Decisions.DecisionNotFound);

        var appeal = decision.Appeals.FirstOrDefault(item => item.AppealId == request.AppealId);
        if (appeal is null)
        {
            if (!await _scope.CanAccessDepartmentAsync(decision.IssuingDepartmentId, cancellationToken))
                return Result.Failure<DocumentDecisionResponse>(DomainErrors.Decisions.AppealOutOfScope);

            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Decisions.AppealNotFound);
        }

        if (!await AppealScope.CoversAsync(
                _scope, decision.IssuingDepartmentId, appeal.AppellantDepartmentId, cancellationToken))
            return Result.Failure<DocumentDecisionResponse>(DomainErrors.Decisions.AppealOutOfScope);

        var recorded = decision.RecordAppealOutcome(
            request.AppealId, request.OutcomeId, request.OutcomeDate, request.UserId, _unitOfWork.UtcNow);
        if (recorded.IsFailure)
            return Result.Failure<DocumentDecisionResponse>(recorded.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var type = await _types.GetByIdAsync(decision.DecisionTypeId, cancellationToken);
        return Result.Success(DecisionMapping.ToResponse(decision, type?.NameAr ?? string.Empty));
    }
}

internal sealed class ListDocumentDecisionsQueryHandler
    : IQueryHandler<ListDocumentDecisionsQuery, IReadOnlyList<DocumentDecisionResponse>>
{
    private readonly IDocumentRepository _documents;
    private readonly IDocumentDecisionRepository _decisions;
    private readonly IDecisionTypeRepository _types;
    private readonly IDocumentAccessEvaluator _access;

    public ListDocumentDecisionsQueryHandler(
        IDocumentRepository documents,
        IDocumentDecisionRepository decisions,
        IDecisionTypeRepository types,
        IDocumentAccessEvaluator access)
    {
        _documents = documents;
        _decisions = decisions;
        _types = types;
        _access = access;
    }

    public async Task<Result<IReadOnlyList<DocumentDecisionResponse>>> Handle(
        ListDocumentDecisionsQuery request,
        CancellationToken cancellationToken)
    {
        if (await _documents.GetByIdAsync(request.DocumentId, cancellationToken) is null)
            return Result.Failure<IReadOnlyList<DocumentDecisionResponse>>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _access.CanAccessDocumentAsync(request.DocumentId, AccessLevel.ViewId, cancellationToken))
            return Result.Failure<IReadOnlyList<DocumentDecisionResponse>>(DomainErrors.Correspondence.DocumentAccessDenied);

        var decisions = await _decisions.ListByDocumentAsync(request.DocumentId, cancellationToken);
        var types = await _types.ListAsync(null, null, cancellationToken);
        var names = types.ToDictionary(type => type.DecisionTypeId, type => type.NameAr);
        IReadOnlyList<DocumentDecisionResponse> responses = decisions
            .Select(decision => DecisionMapping.ToResponse(
                decision, names.GetValueOrDefault(decision.DecisionTypeId, string.Empty)))
            .ToList();
        return Result.Success(responses);
    }
}

internal sealed class GetDocumentNextActionQueryHandler
    : IQueryHandler<GetDocumentNextActionQuery, NextActionResponse>
{
    private readonly IDocumentRepository _documents;
    private readonly IDocumentDecisionRepository _decisions;
    private readonly IDocumentMovementRepository _movements;
    private readonly IInstitutionSettingRepository _settings;
    private readonly IDocumentAccessEvaluator _access;

    public GetDocumentNextActionQueryHandler(
        IDocumentRepository documents,
        IDocumentDecisionRepository decisions,
        IDocumentMovementRepository movements,
        IInstitutionSettingRepository settings,
        IDocumentAccessEvaluator access)
    {
        _documents = documents;
        _decisions = decisions;
        _movements = movements;
        _settings = settings;
        _access = access;
    }

    public async Task<Result<NextActionResponse>> Handle(
        GetDocumentNextActionQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _documents.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<NextActionResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _access.CanAccessDocumentAsync(request.DocumentId, AccessLevel.ViewId, cancellationToken))
            return Result.Failure<NextActionResponse>(DomainErrors.Correspondence.DocumentAccessDenied);

        var custody = await ArchiveDiwanCustody.ResolveAsync(
            document.DocumentId, _settings, _movements, cancellationToken);
        var active = await _decisions.GetActiveByDocumentAsync(document.DocumentId, cancellationToken);
        var visit = await _decisions.FindOpenVisitAsync(document.DocumentId, cancellationToken);

        return Result.Success(NextActionPlanner.Plan(
            document, active, visit, custody.ArchiveDiwanDepartmentId, custody.IsHeld));
    }
}

internal sealed class ListDecisionsQueryHandler
    : IQueryHandler<ListDecisionsQuery, PagedResult<DecisionRegisterItemResponse>>
{
    private readonly IDocumentDecisionRepository _decisions;
    private readonly IDepartmentScopeEvaluator _scope;
    private readonly IUserContext _userContext;
    private readonly IClock _clock;

    public ListDecisionsQueryHandler(
        IDocumentDecisionRepository decisions,
        IDepartmentScopeEvaluator scope,
        IUserContext userContext,
        IClock clock)
    {
        _decisions = decisions;
        _scope = scope;
        _userContext = userContext;
        _clock = clock;
    }

    public async Task<Result<PagedResult<DecisionRegisterItemResponse>>> Handle(
        ListDecisionsQuery request,
        CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize, request.Search, request.SortBy, request.SortDescending);
        var access = new DocumentViewAccess(
            _userContext.UserId,
            await _scope.GetAccessibleDepartmentIdsAsync(cancellationToken),
            _clock.UtcNow);
        var rows = await _decisions.ListPagedAsync(
            page,
            access,
            new DecisionRegisterFilter(request.DecisionTypeId, request.StatusId, request.IssuingDepartmentId, request.Year),
            cancellationToken);

        return Result.Success(new PagedResult<DecisionRegisterItemResponse>(
            rows.Items.Select(row => new DecisionRegisterItemResponse(
                row.Decision.DecisionId,
                row.Decision.DocumentId,
                row.DocumentTitle,
                row.OfficialNumberText,
                row.Decision.DecisionTypeId,
                row.TypeNameAr,
                row.Decision.DecisionNumber,
                row.Decision.DecisionDate,
                row.Decision.DecisionYear,
                row.Decision.IssuingDepartmentId,
                row.Decision.StatusId,
                row.Decision.EffectId,
                row.Decision.IsAppealable,
                row.Decision.DestinationDepartmentId)).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            rows.TotalCount));
    }
}

internal static class AppealScope
{
    public static async Task<bool> CoversAsync(
        IDepartmentScopeEvaluator scope,
        Guid issuingDepartmentId,
        Guid appellantDepartmentId,
        CancellationToken cancellationToken)
    {
        if (await scope.CanAccessDepartmentAsync(issuingDepartmentId, cancellationToken))
            return true;

        return await scope.CanAccessDepartmentAsync(appellantDepartmentId, cancellationToken);
    }
}

internal static class DecisionFileChecks
{
    public static async Task<Result> EnsureAsync(
        IDocumentFileRepository files,
        Guid documentId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var file = await files.GetByIdAsync(fileId, cancellationToken);
        if (file is null || file.DocumentId != documentId || file.FileTypeId != DocumentFileType.DecisionId || file.IsArchived)
            return Result.Failure(DomainErrors.Decisions.DecisionFileInvalid);

        return Result.Success();
    }
}
