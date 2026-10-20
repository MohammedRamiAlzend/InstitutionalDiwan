using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Tests.Support;

internal sealed class FakeDocumentNumberRepository : IDocumentNumberRepository
{
    public List<DocumentNumber> DocumentNumbers { get; } = new();

    public Task<DocumentNumber?> GetByIdAsync(Guid documentNumberId, CancellationToken cancellationToken = default)
        => Task.FromResult(DocumentNumbers.FirstOrDefault(number => number.DocumentNumberId == documentNumberId));

    public Task<DocumentNumber?> GetActiveByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
        => Task.FromResult(DocumentNumbers.FirstOrDefault(
            number => number.DocumentId == documentId &&
                      number.ScopeId == DocumentNumberScope.CentralId &&
                      number.StatusId == DocumentNumberStatus.ActiveId));

    public Task<bool> ExistsByNumberAndYearAsync(
        string officialNumberText,
        int numberingYear,
        CancellationToken cancellationToken = default)
    {
        var text = officialNumberText.Trim();
        return Task.FromResult(DocumentNumbers.Any(
            number => number.OfficialNumberText == text && number.NumberingYear == numberingYear));
    }

    public Task<bool> ExistsInScopeAsync(
        int scopeId,
        Guid registryId,
        string officialNumberText,
        int numberingYear,
        CancellationToken cancellationToken = default)
    {
        var text = officialNumberText.Trim();
        return Task.FromResult(DocumentNumbers.Any(number =>
            number.ScopeId == scopeId &&
            number.RegistryId == registryId &&
            number.OfficialNumberText == text &&
            number.NumberingYear == numberingYear));
    }

    public Task<DocumentNumber?> GetDepartmentEntryAsync(
        Guid documentId,
        Guid departmentId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(DocumentNumbers.FirstOrDefault(number =>
            number.DocumentId == documentId &&
            number.ScopeId == DocumentNumberScope.DepartmentId &&
            number.DepartmentId == departmentId));

    public Task<IReadOnlyList<DocumentNumber>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DocumentNumber>>(
            DocumentNumbers.Where(number => number.DocumentId == documentId)
                .OrderBy(number => number.IssuedAt)
                .ToList());

    public Task<IReadOnlyList<DocumentNumber>> ListByDepartmentAsync(
        Guid departmentId,
        int? numberingYear,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DocumentNumber>>(
            DocumentNumbers.Where(number => number.DepartmentId == departmentId &&
                                            (numberingYear == null || number.NumberingYear == numberingYear))
                .OrderBy(number => number.RegistryDate)
                .ToList());

    public Task<bool> AnyByRegistryAsync(Guid registryId, CancellationToken cancellationToken = default)
        => Task.FromResult(DocumentNumbers.Any(number =>
            number.RegistryId == registryId && number.ScopeId == DocumentNumberScope.CentralId));

    public void Add(DocumentNumber documentNumber) => DocumentNumbers.Add(documentNumber);
}

internal sealed class FakeNumberingRegistryRepository : INumberingRegistryRepository
{
    public List<NumberingRegistry> Registries { get; } = new();

    public Task<NumberingRegistry?> GetByIdAsync(Guid registryId, CancellationToken cancellationToken = default)
        => Task.FromResult(Registries.FirstOrDefault(registry => registry.RegistryId == registryId));

    public Task<IReadOnlyList<NumberingRegistry>> ListAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<NumberingRegistry>>(
            Registries.OrderBy(registry => registry.Code).ToList());

    public Task<NumberingRegistry?> GetActiveForScopeAsync(
        Guid? departmentId,
        int? documentTypeId,
        int? directionId,
        CancellationToken cancellationToken = default)
    {
        var candidates = Registries
            .Where(registry => registry.IsActive &&
                               (registry.DepartmentId == null || registry.DepartmentId == departmentId) &&
                               (registry.DocumentTypeId == null || registry.DocumentTypeId == documentTypeId) &&
                               (registry.DirectionId == null || registry.DirectionId == directionId))
            .ToList();

        var selected = candidates
            .OrderBy(registry =>
            {
                var specificity = 0;
                if (registry.DepartmentId is not null && registry.DepartmentId == departmentId)
                    specificity++;
                if (registry.DocumentTypeId is not null && registry.DocumentTypeId == documentTypeId)
                    specificity++;
                if (registry.DirectionId is not null && registry.DirectionId == directionId)
                    specificity++;
                return specificity;
            })
            .ThenBy(registry => registry.CreatedAt)
            .FirstOrDefault();

        return Task.FromResult(selected);
    }

    public Task<NumberingRegistry?> GetActiveDepartmentRegistryAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Registries
            .Where(registry => registry.IsActive && registry.DepartmentId == departmentId)
            .OrderByDescending(registry =>
                (registry.DocumentTypeId is null ? 0 : 1) + (registry.DirectionId is null ? 0 : 1))
            .ThenBy(registry => registry.CreatedAt)
            .FirstOrDefault());

    public void Add(NumberingRegistry registry) => Registries.Add(registry);
}
