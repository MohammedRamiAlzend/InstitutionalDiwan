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
            number => number.DocumentId == documentId && number.Status == "ACTIVE"));

    public Task<bool> ExistsByNumberAndYearAsync(
        string officialNumberText,
        int numberingYear,
        CancellationToken cancellationToken = default)
    {
        var text = officialNumberText.Trim();
        return Task.FromResult(DocumentNumbers.Any(
            number => number.OfficialNumberText == text && number.NumberingYear == numberingYear));
    }

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
        string? documentTypeCode,
        string? direction,
        CancellationToken cancellationToken = default)
    {
        var normalizedDocumentType = string.IsNullOrWhiteSpace(documentTypeCode)
            ? null
            : documentTypeCode.Trim().ToUpperInvariant();
        var normalizedDirection = string.IsNullOrWhiteSpace(direction)
            ? null
            : direction.Trim().ToUpperInvariant();

        var candidates = Registries
            .Where(registry => registry.IsActive &&
                               (registry.DepartmentId == null || registry.DepartmentId == departmentId) &&
                               (registry.DocumentTypeCode == null || registry.DocumentTypeCode == normalizedDocumentType) &&
                               (registry.Direction == null || registry.Direction == normalizedDirection))
            .ToList();

        var selected = candidates
            .OrderBy(registry =>
            {
                var specificity = 0;
                if (registry.DepartmentId is not null && registry.DepartmentId == departmentId)
                    specificity++;
                if (registry.DocumentTypeCode is not null && registry.DocumentTypeCode == normalizedDocumentType)
                    specificity++;
                if (registry.Direction is not null && registry.Direction == normalizedDirection)
                    specificity++;
                return specificity;
            })
            .ThenBy(registry => registry.CreatedAt)
            .FirstOrDefault();

        return Task.FromResult(selected);
    }

    public void Add(NumberingRegistry registry) => Registries.Add(registry);
}
