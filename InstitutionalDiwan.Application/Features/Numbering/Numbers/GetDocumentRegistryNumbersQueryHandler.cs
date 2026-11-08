using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

internal sealed class GetDocumentRegistryNumbersQueryHandler
    : IQueryHandler<GetDocumentRegistryNumbersQuery, DocumentRegistryNumbersResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;

    public GetDocumentRegistryNumbersQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository,
        IDocumentAccessEvaluator documentAccessEvaluator)
    {
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<Result<DocumentRegistryNumbersResponse>> Handle(
        GetDocumentRegistryNumbersQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentRegistryNumbersResponse>(
                DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.ViewId, cancellationToken))
        {
            return Result.Failure<DocumentRegistryNumbersResponse>(
                DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var numbers = await _documentNumberRepository.ListByDocumentAsync(
            request.DocumentId, cancellationToken);

        var central = numbers.FirstOrDefault(number =>
            number.ScopeId == DocumentNumberScope.CentralId &&
            number.DocumentNumberId == document.ActiveOfficialNumberId);

        var departmentNumbers = numbers
            .Where(number => number.ScopeId == DocumentNumberScope.DepartmentId)
            .OrderBy(number => number.RegistryDate)
            .ThenBy(number => number.OfficialNumberText)
            .Select(number => new DepartmentRegistryNumberResponse(
                number.DocumentNumberId,
                number.DepartmentId!.Value,
                number.RegistryId,
                number.OfficialNumberText,
                number.NumberingYear,
                number.RegistryDate,
                number.StatusId,
                number.History
                    .OrderBy(history => history.OccurredAt)
                    .Select(history => new DepartmentRegistrationEventResponse(
                        history.HistoryId,
                        history.EventTypeId,
                        history.EffectiveDate,
                        history.OccurredAt,
                        history.ActorUserId,
                        history.Notes))
                    .ToList(),
                number.Visits
                    .OrderBy(visit => visit.RegistryDate)
                    .Select(visit => new DepartmentRegistryVisitResponse(
                        visit.VisitId,
                        visit.RegistryDate,
                        visit.MovementStepId,
                        visit.RecordedByUserId,
                        visit.RecordedAt,
                        visit.Notes))
                    .ToList()))
            .ToList();

        return Result.Success(new DocumentRegistryNumbersResponse(
            document.DocumentId,
            central?.DocumentNumberId,
            document.OfficialNumberText,
            central?.RegistryDate ?? document.RegistrationDate,
            departmentNumbers));
    }
}
