using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

internal sealed class ListDepartmentRegisterQueryHandler
    : IQueryHandler<ListDepartmentRegisterQuery, IReadOnlyCollection<DepartmentRegisterLineResponse>>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;

    public ListDepartmentRegisterQueryHandler(
        IDepartmentRepository departmentRepository,
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository)
    {
        _departmentRepository = departmentRepository;
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
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

        var numbers = await _documentNumberRepository.ListByDepartmentAsync(
            request.DepartmentId, request.NumberingYear, cancellationToken);

        if (numbers.Count == 0)
            return Result.Success<IReadOnlyCollection<DepartmentRegisterLineResponse>>(
                Array.Empty<DepartmentRegisterLineResponse>());

        var documents = await _documentRepository.GetByIdsAsync(
            numbers.Select(number => number.DocumentId).Distinct().ToList(), cancellationToken);

        var documentLookup = documents.ToDictionary(document => document.DocumentId);

        IReadOnlyCollection<DepartmentRegisterLineResponse> register = numbers
            .Select(number =>
            {
                documentLookup.TryGetValue(number.DocumentId, out var document);

                return new DepartmentRegisterLineResponse(
                    number.DocumentNumberId,
                    number.DocumentId,
                    document?.Title ?? string.Empty,
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
