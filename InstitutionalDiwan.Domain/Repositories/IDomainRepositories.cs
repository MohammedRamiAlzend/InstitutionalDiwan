

namespace InstitutionalDiwan.Domain.Repositories;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<Department?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> HasActiveChildrenAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Department>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetDescendantIdsAsync(Guid departmentId, CancellationToken cancellationToken = default);
    void Add(Department department);
}

public interface IDepartmentTypeRepository
{
    Task<DepartmentType?> GetByIdAsync(int departmentTypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentType>> ListAllAsync(CancellationToken cancellationToken = default);
    void Add(DepartmentType departmentType);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> ListAllAsync(CancellationToken cancellationToken = default);
    void Add(User user);
}

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> ListAllAsync(CancellationToken cancellationToken = default);
    void Add(Role role);
}

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> ListAllAsync(CancellationToken cancellationToken = default);
    void Add(Employee employee);
}

public interface IDocumentAccessGrantRepository
{
    Task<DocumentAccessGrant?> GetByIdAsync(Guid grantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentAccessGrant>> ListByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    void Add(DocumentAccessGrant grant);
}

public interface IPermissionRepository
{
    Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> ListAllAsync(CancellationToken cancellationToken = default);
}

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    /// <summary>Loads the document together with its central and department numbers.</summary>
    Task<Document?> GetWithNumbersAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> ListRecentAsync(int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetByIdsAsync(
        IReadOnlyCollection<Guid> documentIds,
        CancellationToken cancellationToken = default);
    void Add(Document document);
}

public interface IDocumentTypeRepository
{
    Task<DocumentType?> GetByIdAsync(int documentTypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentType>> ListAllAsync(CancellationToken cancellationToken = default);
    void Add(DocumentType documentType);
}

public interface INumberingRegistryRepository
{
    Task<NumberingRegistry?> GetByIdAsync(Guid registryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NumberingRegistry>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<NumberingRegistry?> GetActiveForScopeAsync(Guid? departmentId, int? documentTypeId, int? directionId, CancellationToken cancellationToken = default);
    /// <summary>The active registry owned by <paramref name="departmentId"/>; never a shared default.</summary>
    Task<NumberingRegistry?> GetActiveDepartmentRegistryAsync(Guid departmentId, CancellationToken cancellationToken = default);
    void Add(NumberingRegistry registry);
}

public interface IDocumentNumberRepository
{
    Task<DocumentNumber?> GetByIdAsync(Guid documentNumberId, CancellationToken cancellationToken = default);
    Task<DocumentNumber?> GetActiveByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNumberAndYearAsync(string officialNumberText, int numberingYear, CancellationToken cancellationToken = default);
    /// <summary>Uniqueness check scoped to one register: central or a single department.</summary>
    Task<bool> ExistsInScopeAsync(
        int scopeId,
        Guid registryId,
        string officialNumberText,
        int numberingYear,
        CancellationToken cancellationToken = default);
    Task<DocumentNumber?> GetDepartmentEntryAsync(
        Guid documentId,
        Guid departmentId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentNumber>> ListByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    /// <summary>The register book: one department's entries, oldest register date first.</summary>
    Task<IReadOnlyList<DocumentNumber>> ListByDepartmentAsync(
        Guid departmentId,
        int? numberingYear,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// True when the registry allocated a central number. Department entries are typed, never
    /// formatted, so they must not freeze the registry template.
    /// </summary>
    Task<bool> AnyByRegistryAsync(Guid registryId, CancellationToken cancellationToken = default);
    void Add(DocumentNumber documentNumber);
}

public interface IInstitutionSettingRepository
{
    Task<InstitutionSetting?> GetSingletonAsync(CancellationToken cancellationToken = default);
    void Add(InstitutionSetting setting);
}

public interface IExternalOrganizationRepository
{
    Task<ExternalOrganization?> GetByIdAsync(Guid externalOrganizationId, CancellationToken cancellationToken = default);
    Task<ExternalOrganization?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExternalOrganization>> ListAllAsync(CancellationToken cancellationToken = default);
    void Add(ExternalOrganization externalOrganization);
}

public interface IDocumentMovementRepository
{
    Task<DocumentMovement?> GetByIdAsync(Guid movementId, CancellationToken cancellationToken = default);
    /// <summary>Every movement of a document, newest first, with its steps and events.</summary>
    Task<IReadOnlyList<DocumentMovement>> ListByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Movements waiting in a department's inbox, newest first: the current step is addressed to
    /// that department and has not left it yet, plus the documents that came back to it because
    /// the receiving department rejected or returned them.
    /// </summary>
    Task<IReadOnlyList<DocumentMovement>> ListInboxAsync(Guid departmentId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Movements a department sent out and that are still with the receiving side, newest first:
    /// the current step leaves that department and has not come back.
    /// </summary>
    Task<IReadOnlyList<DocumentMovement>> ListOutboxAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveMovementsAsync(Guid documentId, CancellationToken cancellationToken = default);
    void Add(DocumentMovement movement);
}

public interface IDocumentFileRepository
{
    Task<DocumentFile?> GetByIdAsync(Guid fileId, CancellationToken cancellationToken = default);
    void Add(DocumentFile documentFile);
}

public interface IDocumentRelationRepository
{
    Task<DocumentRelation?> GetByIdAsync(Guid relationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentRelation>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    void Add(DocumentRelation relation);
    void Remove(DocumentRelation relation);
}

public interface IAuditLogRepository
{
    Task<AuditLog?> GetLatestAsync(CancellationToken cancellationToken = default);
    void Add(AuditLog auditLog);
}

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken = default);
    void Add(Notification notification);
}

/// <summary>
/// Generic access to the seeded reference ("lookup") catalogs. Implementations own the
/// slug-to-entity registry, so callers address a catalog by its kebab-case key
/// (<c>document-types</c>, <c>movement-statuses</c>, …) instead of a concrete entity type.
/// </summary>
public interface ILookupRepository
{
    /// <summary>True when <paramref name="lookupKey"/> maps to a known lookup catalog.</summary>
    bool IsSupported(string lookupKey);

    /// <summary>Returns all supported lookup catalog keys.</summary>
    IReadOnlyList<string> GetSupportedCatalogs();

    /// <summary>Lists a catalog ordered by id, optionally including deactivated entries.</summary>
    Task<IReadOnlyList<LookupEntity>> ListAsync(
        string lookupKey,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    /// <summary>Loads a single tracked entry so it can be relabelled or (de)activated.</summary>
    Task<LookupEntity?> GetByIdAsync(
        string lookupKey,
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>Creates and tracks a new catalog entry; fails when the id is taken or the labels are blank.</summary>
    Task<Result<LookupEntity>> AddAsync(
        string lookupKey,
        int id,
        string desc,
        string descAr,
        CancellationToken cancellationToken = default);

    /// <summary>Updates an entry's English/Arabic labels, or returns null when it does not exist.</summary>
    Task<LookupEntity?> RelabelAsync(
        string lookupKey,
        int id,
        string desc,
        string descAr,
        CancellationToken cancellationToken = default);

    /// <summary>Activates or deactivates an entry, or returns null when it does not exist.</summary>
    Task<LookupEntity?> SetActiveStateAsync(
        string lookupKey,
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);

    /// <summary>True when any table holds a foreign key to this entry, so it must not be deactivated.</summary>
    Task<bool> IsInUseAsync(string lookupKey, int id, CancellationToken cancellationToken = default);
}
