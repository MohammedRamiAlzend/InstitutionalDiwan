

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
    /// <summary>A page of users ordered by username, optionally filtered by a username search term.</summary>
    Task<PagedList<User>> ListPagedAsync(PageRequest page, CancellationToken cancellationToken = default);
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
    /// <summary>
    /// A page of employees ordered by Arabic name, optionally filtered by a name or employee-number
    /// search term and restricted to the given departments. A null set means no department filter;
    /// an empty set means nothing can match.
    /// </summary>
    Task<PagedList<Employee>> ListPagedAsync(
        PageRequest page,
        IReadOnlyCollection<Guid>? departmentIds = null,
        CancellationToken cancellationToken = default);
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
    /// <summary>
    /// A page of documents with the optional status, owning-department, direction, and type filters
    /// applied. <see cref="PageRequest.SortBy"/> accepts <c>createdAt</c>, <c>title</c>,
    /// <c>registrationDate</c>, and <c>officialNumber</c>; the default is newest first.
    /// </summary>
    Task<PagedList<Document>> ListPagedAsync(
        PageRequest page,
        int? statusId = null,
        Guid? owningDepartmentId = null,
        int? directionId = null,
        int? documentTypeId = null,
        CancellationToken cancellationToken = default);
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
    /// <summary>
    /// A page of organizations ordered by code, optionally filtered by a code/name search term and
    /// restricted to active organizations unless <paramref name="includeInactive"/> is true.
    /// </summary>
    Task<PagedList<ExternalOrganization>> ListPagedAsync(
        PageRequest page,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
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
    /// <summary>The non-terminal movement that currently holds the document, if any.</summary>
    Task<DocumentMovement?> GetActiveByDocumentIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
    Task<bool> HasActiveMovementsAsync(Guid documentId, CancellationToken cancellationToken = default);
    void Add(DocumentMovement movement);
}

public interface IDocumentFileRepository
{
    /// <summary>Loads a file with every version it has held, current first in version order.</summary>
    Task<DocumentFile?> GetByIdAsync(Guid fileId, CancellationToken cancellationToken = default);
    /// <summary>Every file attached to a document, oldest first, with their versions.</summary>
    Task<IReadOnlyList<DocumentFile>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
    void Add(DocumentFile documentFile);
}

public interface IDocumentFileTypeRepository
{
    Task<DocumentFileType?> GetByIdAsync(int fileTypeId, CancellationToken cancellationToken = default);
}

public interface IFileStorageProviderRepository
{
    Task<FileStorageProvider?> GetByIdAsync(int storageProviderId, CancellationToken cancellationToken = default);
    /// <summary>The active provider marked as default; the first active local provider as a fallback.</summary>
    Task<FileStorageProvider?> GetDefaultAsync(CancellationToken cancellationToken = default);
}

public interface IDocumentRelationRepository
{
    Task<DocumentRelation?> GetByIdAsync(Guid relationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentRelation>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    void Add(DocumentRelation relation);
    void Remove(DocumentRelation relation);
}

/// <summary>
/// Filter applied to <see cref="IAuditLogRepository.SearchAsync"/>. All values are optional; a null
/// value means "do not filter on this column".
/// </summary>
public sealed record AuditLogFilter(
    string? ModuleName = null,
    int? ActionTypeId = null,
    int? EntityTypeId = null,
    string? EntityId = null,
    Guid? ActorUserId = null,
    Guid? ActorDepartmentId = null,
    Guid? RelatedDocumentId = null,
    string? CorrelationId = null,
    string? IpAddress = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? Search = null);

public interface IAuditLogRepository
{
    /// <summary>The last appended audit entry, or null when the chain is empty.</summary>
    Task<AuditLog?> GetLatestAsync(CancellationToken cancellationToken = default);

    Task<AuditLog?> GetByIdAsync(long auditLogId, CancellationToken cancellationToken = default);

    /// <summary>Filtered, paged audit entries, newest first unless <paramref name="page"/> requests otherwise.</summary>
    Task<PagedList<AuditLog>> SearchAsync(
        AuditLogFilter filter,
        PageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary>Every entry between two ids (inclusive), ordered by id, for chain verification.</summary>
    Task<IReadOnlyList<AuditLog>> ListRangeAsync(
        long fromAuditLogId,
        long toAuditLogId,
        CancellationToken cancellationToken = default);

    Task<AuditCheckpoint?> GetLatestCheckpointAsync(CancellationToken cancellationToken = default);

    Task<PagedList<AuditCheckpoint>> ListCheckpointsAsync(
        PageRequest page,
        CancellationToken cancellationToken = default);

    Task<PagedList<AuditIntegrityCheck>> ListIntegrityChecksAsync(
        PageRequest page,
        CancellationToken cancellationToken = default);

    void Add(AuditLog auditLog);
    void AddCheckpoint(AuditCheckpoint checkpoint);
    void AddIntegrityCheck(AuditIntegrityCheck integrityCheck);
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
