using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Tests.Support;

internal sealed class FakeInstitutionSettingRepository : IInstitutionSettingRepository
{
    public List<InstitutionSetting> Settings { get; } = new();

    public Task<InstitutionSetting?> GetSingletonAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Settings.FirstOrDefault());

    public void Add(InstitutionSetting setting) => Settings.Add(setting);
}

internal sealed class FakeExternalOrganizationRepository : IExternalOrganizationRepository
{
    public List<ExternalOrganization> Organizations { get; } = new();

    public Task<ExternalOrganization?> GetByIdAsync(
        Guid externalOrganizationId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Organizations.FirstOrDefault(
            organization => organization.ExternalOrganizationId == externalOrganizationId));

    public Task<ExternalOrganization?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return Task.FromResult(Organizations.FirstOrDefault(
            organization => organization.Code == normalized));
    }

    public Task<IReadOnlyList<ExternalOrganization>> ListAllAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ExternalOrganization>>(
            Organizations.OrderBy(organization => organization.Code).ToList());

    public void Add(ExternalOrganization externalOrganization) =>
        Organizations.Add(externalOrganization);
}

internal sealed class FakeDocumentMovementRepository : IDocumentMovementRepository
{
    public List<DocumentMovement> Movements { get; } = new();
    public HashSet<Guid> DocumentIdsWithActiveMovements { get; } = new();

    public Task<DocumentMovement?> GetByIdAsync(Guid movementId, CancellationToken cancellationToken = default)
        => Task.FromResult(Movements.FirstOrDefault(movement => movement.MovementId == movementId));

    public Task<bool> HasActiveMovementsAsync(Guid documentId, CancellationToken cancellationToken = default)
        => Task.FromResult(DocumentIdsWithActiveMovements.Contains(documentId));

    public void Add(DocumentMovement movement)
    {
        Movements.Add(movement);
        DocumentIdsWithActiveMovements.Add(movement.DocumentId);
    }
}

internal sealed class FakeLookupRepository : ILookupRepository
{
    private readonly Dictionary<string, List<LookupEntity>> _entries = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string lookupKey, LookupEntity entry)
    {
        if (!_entries.TryGetValue(lookupKey, out var list))
        {
            list = new List<LookupEntity>();
            _entries[lookupKey] = list;
        }

        list.Add(entry);
    }

    public bool IsSupported(string lookupKey) => _entries.ContainsKey(lookupKey);

    public IReadOnlyList<string> GetSupportedCatalogs() => _entries.Keys.ToList();

    public Task<IReadOnlyList<LookupEntity>> ListAsync(
        string lookupKey,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var entries = _entries.TryGetValue(lookupKey, out var list)
            ? list.Where(entry => includeInactive || entry.IsActive).OrderBy(entry => entry.Id).ToList()
            : new List<LookupEntity>();

        return Task.FromResult<IReadOnlyList<LookupEntity>>(entries);
    }

    public Task<LookupEntity?> GetByIdAsync(
        string lookupKey,
        int id,
        CancellationToken cancellationToken = default)
    {
        var entry = _entries.TryGetValue(lookupKey, out var list)
            ? list.FirstOrDefault(candidate => candidate.Id == id)
            : null;

        return Task.FromResult(entry);
    }

    public Task<Result<LookupEntity>> AddAsync(
        string lookupKey,
        int id,
        string desc,
        string descAr,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("The fake lookup catalog is read-only.");

    public Task<LookupEntity?> RelabelAsync(
        string lookupKey,
        int id,
        string desc,
        string descAr,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("The fake lookup catalog is read-only.");

    public Task<LookupEntity?> SetActiveStateAsync(
        string lookupKey,
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("The fake lookup catalog is read-only.");

    public Task<bool> IsInUseAsync(string lookupKey, int id, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}

/// <summary>Ambient identity driven by the test, mirroring the JWT claims the real one reads.</summary>
internal sealed class FakeUserContext : IUserContext
{
    public bool IsAuthenticated { get; set; }
    public Guid UserId { get; set; }
    public Guid PrimaryDepartmentId { get; set; }
    public int ScopeTypeId { get; set; } = UserScopeType.DepartmentId;
    public IReadOnlySet<string> Permissions { get; set; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

internal sealed class FakeDepartmentScopeEvaluator : IDepartmentScopeEvaluator
{
    public HashSet<Guid> AccessibleDepartments { get; } = new();

    public Task<bool> CanAccessDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(AccessibleDepartments.Contains(departmentId));
}
