using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

/// <summary>
/// A department's register book. The department must sit inside the caller's scope. Every line stays,
/// because the book must show no gaps and its number belongs to the department, but the document's
/// title and central number appear only when the caller may view that document.
/// </summary>
internal sealed class ListDepartmentRegisterQueryHandler
    : IQueryHandler<ListDepartmentRegisterQuery, IReadOnlyCollection<DepartmentRegisterLineResponse>>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUserContext _userContext;
    private readonly IClock _clock;

    public ListDepartmentRegisterQueryHandler(
        IDepartmentRepository departmentRepository,
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUserContext userContext,
        IClock clock)
    {
        _departmentRepository = departmentRepository;
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _userContext = userContext;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyCollection<DepartmentRegisterLineResponse>>> Handle(
        ListDepartmentRegisterQuery request,
        CancellationToken cancellationToken)
    {
        if (await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken) is null)
        {
            return Result.Failure<IReadOnlyCollection<DepartmentRegisterLineResponse>>(
                DomainErrors.Department.NotFound);
        }

        if (!await _departmentScopeEvaluator.CanAccessDepartmentAsync(request.DepartmentId, cancellationToken))
        {
            return Result.Failure<IReadOnlyCollection<DepartmentRegisterLineResponse>>(
                DomainErrors.Correspondence.DepartmentOutOfScope);
        }

        var numbers = await _documentNumberRepository.ListByDepartmentAsync(
            request.DepartmentId, request.NumberingYear, cancellationToken);

        if (numbers.Count == 0)
            return Result.Success<IReadOnlyCollection<DepartmentRegisterLineResponse>>(
                Array.Empty<DepartmentRegisterLineResponse>());

        var documentIds = numbers.Select(number => number.DocumentId).Distinct().ToList();

        var documents = await _documentRepository.GetByIdsAsync(documentIds, cancellationToken);

        var documentLookup = documents.ToDictionary(document => document.DocumentId);

        var viewableIds = await _documentRepository.ListViewableIdsAsync(
            documentIds,
            new DocumentViewAccess(
                _userContext.UserId,
                await _departmentScopeEvaluator.GetAccessibleDepartmentIdsAsync(cancellationToken),
                _clock.UtcNow),
            cancellationToken);

        IReadOnlyCollection<DepartmentRegisterLineResponse> register = numbers
            .Select(number =>
            {
                var document = viewableIds.Contains(number.DocumentId)
                    ? documentLookup.GetValueOrDefault(number.DocumentId)
                    : null;

                return new DepartmentRegisterLineResponse(
                    number.DocumentNumberId,
                    number.DocumentId,
                    document?.DisplayName ?? string.Empty,
                    document?.OfficialNumberText,
                    number.OfficialNumberText,
                    number.NumberingYear,
                    number.RegistryDate,
                    number.StatusId,
                    number.History.Count(history =>
                        history.EventTypeId is DocumentNumberEventType.RegisteredInDepartmentId
                            or DocumentNumberEventType.ReRegisteredInDepartmentId));
            })
            .ToList();

        return Result.Success(register);
    }
}
