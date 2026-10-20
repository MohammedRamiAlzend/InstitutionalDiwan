using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

internal sealed class GetDocumentRegistryNumbersQueryHandler
    : IQueryHandler<GetDocumentRegistryNumbersQuery, DocumentRegistryNumbersResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;

    public GetDocumentRegistryNumbersQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository)
    {
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
    }

    public async Task<Result<DocumentRegistryNumbersResponse>> Handle(
        GetDocumentRegistryNumbersQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentRegistryNumbersResponse>(
                DomainErrors.Correspondence.DocumentNotFound);

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
