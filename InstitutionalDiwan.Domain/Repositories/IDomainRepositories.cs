using InstitutionalDiwan.Domain.Entities.Diagnostics;


using InstitutionalDiwan.Domain.ValueObjects;

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
    /// <summary>The users with the given ids (no related data), for labelling references.</summary>
    Task<IReadOnlyList<User>> ListByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);
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

/// <summary>
/// Who is reading a list of documents, for applying the <c>VIEW</c> rule inside the query.
/// <paramref name="AccessibleDepartmentIds"/> is the caller's organizational scope, where null means
/// institution-wide. <paramref name="AsOfUtc"/> decides which access grants are still valid.
/// </summary>
public sealed record DocumentViewAccess(
    Guid CallerUserId,
    IReadOnlyCollection<Guid>? AccessibleDepartmentIds,
    DateTime AsOfUtc);

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    /// <summary>Loads the document together with its central and department numbers.</summary>
    Task<Document?> GetWithNumbersAsync(Guid documentId, CancellationToken cancellationToken = default);
    /// <summary>
    /// A page of the documents the caller may view, with the optional status, owning-department,
    /// direction, and type filters applied. Access is part of the query, so paging counters never
    /// include a document the caller cannot see. <see cref="PageRequest.SortBy"/> accepts
    /// <c>createdAt</c>, <c>title</c>, <c>registrationDate</c>, and <c>officialNumber</c>; the
    /// default is newest first.
    /// <c>registerNumber</c> keeps documents with a line in any department register whose number contains it.
    /// </summary>
    Task<PagedList<Document>> ListPagedAsync(
        PageRequest page,
        DocumentViewAccess access,
        int? statusId = null,
        Guid? owningDepartmentId = null,
        int? directionId = null,
        int? documentTypeId = null,
        IReadOnlyList<DocumentFieldFilter>? fieldFilters = null,
        DocumentFieldSort? fieldSort = null,
        string? registerNumber = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetByIdsAsync(
        IReadOnlyCollection<Guid> documentIds,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Which of the given documents the caller may view, for rows that must stay listed (a queue, a
    /// register) but may only show a document's content to someone who can read it.
    /// </summary>
    Task<IReadOnlySet<Guid>> ListViewableIdsAsync(
        IReadOnlyCollection<Guid> documentIds,
        DocumentViewAccess access,
        CancellationToken cancellationToken = default);
    /// <summary>The stored values of the given fields for the given documents; callers pass only documents the caller may view.</summary>
    Task<IReadOnlyList<DocumentFieldValue>> ListFieldValuesAsync(
        IReadOnlyCollection<Guid> documentIds,
        IReadOnlyCollection<Guid> documentFieldIds,
        CancellationToken cancellationToken = default);
    void Add(Document document);
}

public interface IDocumentTypeRepository
{
    Task<DocumentType?> GetByIdAsync(int documentTypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentType>> ListAllAsync(CancellationToken cancellationToken = default);
    void Add(DocumentType documentType);
}

public sealed record SystemErrorFilter(
    SystemErrorStatus? Status = null,
    SystemErrorSource? Source = null,
    SystemErrorKind? Kind = null,
    DateTime? LastSeenFromUtc = null,
    DateTime? LastSeenToUtc = null);

public interface ISystemErrorRepository
{
    Task<SystemError?> GetByIdAsync(Guid systemErrorId, CancellationToken cancellationToken = default);
    Task<PagedList<SystemError>> ListPagedAsync(PageRequest page, SystemErrorFilter filter, CancellationToken cancellationToken = default);
    /// <summary>Filtered and sorted like the list, without paging, up to <paramref name="maxRows"/>.</summary>
    Task<IReadOnlyList<SystemError>> ListForExportAsync(PageRequest page, SystemErrorFilter filter, int maxRows, CancellationToken cancellationToken = default);
    /// <summary>Permanently deletes the errors last seen before the cutoff and returns how many.</summary>
    Task<int> DeleteLastSeenBeforeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}

/// <summary>
/// Fields of one form of a document type. The overloads without a department read the creation form;
/// those with one read that department's record form (a null department is the creation form too).
/// </summary>
public interface IDocumentFieldRepository
{
    Task<DocumentField?> GetByIdAsync(Guid documentFieldId, CancellationToken cancellationToken = default);
    Task<DocumentField?> GetByKeyAsync(int documentTypeId, string key, CancellationToken cancellationToken = default);
    Task<DocumentField?> GetByKeyAsync(int documentTypeId, Guid? departmentId, string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentField>> ListByDocumentTypeAsync(int documentTypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentField>> ListByDocumentTypeAsync(int documentTypeId, Guid? departmentId, CancellationToken cancellationToken = default);
    void Add(DocumentField field);
}

/// <summary>
/// Schema versions of one form of a document type. The overloads without a department read the
/// creation form; those with one read that department's record form (null is the creation form).
/// </summary>
public interface IDocumentTypeSchemaVersionRepository
{
    /// <summary>A version with its fields, by id.</summary>
    Task<DocumentTypeSchemaVersion?> GetByIdAsync(Guid schemaVersionId, CancellationToken cancellationToken = default);
    /// <summary>The single editable draft of a document type's creation form, if one exists.</summary>
    Task<DocumentTypeSchemaVersion?> GetDraftAsync(int documentTypeId, CancellationToken cancellationToken = default);
    Task<DocumentTypeSchemaVersion?> GetDraftAsync(int documentTypeId, Guid? departmentId, CancellationToken cancellationToken = default);
    /// <summary>The published creation form new documents of the type are pinned to, if any.</summary>
    Task<DocumentTypeSchemaVersion?> GetPublishedAsync(int documentTypeId, CancellationToken cancellationToken = default);
    Task<DocumentTypeSchemaVersion?> GetPublishedAsync(int documentTypeId, Guid? departmentId, CancellationToken cancellationToken = default);
    /// <summary>The highest version number used by the form (0 when none), for numbering the next draft.</summary>
    Task<int> GetLatestVersionNumberAsync(int documentTypeId, CancellationToken cancellationToken = default);
    Task<int> GetLatestVersionNumberAsync(int documentTypeId, Guid? departmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentTypeSchemaVersion>> ListByDocumentTypeAsync(int documentTypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentTypeSchemaVersion>> ListByDocumentTypeAsync(int documentTypeId, Guid? departmentId, CancellationToken cancellationToken = default);
    /// <summary>Every published or retired record-form version of one department, all document types, newest first.</summary>
    Task<IReadOnlyList<DocumentTypeSchemaVersion>> ListIssuedByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);
    void Add(DocumentTypeSchemaVersion version);
    /// <summary>Deletes a discarded draft together with its fields; published versions are never removed.</summary>
    void Remove(DocumentTypeSchemaVersion version);
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

/// <summary>What narrows a department's register page. Dates compare by calendar day, both ends inclusive.</summary>
public sealed record DepartmentRegisterFilter(
    Guid DepartmentId,
    Guid? RegistryId = null,
    int? NumberingYear = null,
    int? StatusId = null,
    DateTime? RegistryDateFrom = null,
    DateTime? RegistryDateTo = null,
    int? DocumentTypeId = null,
    Guid? SchemaVersionId = null,
    DocumentFieldSort? FieldSort = null,
    IReadOnlyList<DocumentFieldFilter>? FieldFilters = null);

/// <summary>One line of a department register page, with what a screen needs beside the line itself.</summary>
public sealed record DepartmentRegisterRow(
    DocumentNumber Number,
    string? DocumentTitle,
    string? CentralNumberText,
    int RegistrationCount,
    string? ReplacedByNumberText,
    string? ReplacesNumberText,
    DocumentRegistryVisit? RecordVisit);

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
    /// <summary>The document whose register line holds visit <paramref name="visitId"/>, if any.</summary>
    Task<Guid?> FindDocumentIdByVisitAsync(Guid visitId, CancellationToken cancellationToken = default);
    /// <summary>
    /// One page of a department's register lines, narrowed and ordered in the database. A line whose
    /// document the caller cannot view keeps its number but has no title and does not match a title search.
    /// When the filter names a record form version, only lines with a visit on that version are returned,
    /// each with its latest such visit and the visit's values.
    /// </summary>
    Task<PagedList<DepartmentRegisterRow>> ListDepartmentRegisterPagedAsync(
        PageRequest page,
        DocumentViewAccess access,
        DepartmentRegisterFilter filter,
        CancellationToken cancellationToken = default);
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
    Task<InstitutionLogo?> GetLogoAsync(CancellationToken cancellationToken = default);
    void AddLogo(InstitutionLogo logo);
    void RemoveLogo(InstitutionLogo logo);
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

/// <summary>
/// Filter applied to <see cref="IDocumentArchiveRepository.SearchAsync"/>. <paramref name="CallerUserId"/>
/// and <paramref name="AccessibleDepartmentIds"/> restrict the result to what the caller may view; a null
/// set means an institution-wide scope. <paramref name="AsOfUtc"/> decides which access grants are still
/// valid. The remaining values are optional narrowing filters.
/// </summary>
public sealed record ArchiveSearchFilter(
    Guid CallerUserId,
    IReadOnlyCollection<Guid>? AccessibleDepartmentIds,
    DateTime AsOfUtc,
    Guid? OwningDepartmentId = null,
    int? DocumentTypeId = null,
    int? DirectionId = null,
    int? ArchiveLocationId = null,
    DateTime? ArchivedFromUtc = null,
    DateTime? ArchivedToUtc = null,
    IReadOnlyList<DocumentFieldFilter>? FieldFilters = null);

/// <summary>An archived document together with the details of the event that archived it.</summary>
public sealed record ArchivedDocumentRow(
    Document Document,
    DateTime ArchivedAt,
    Guid ArchivedByUserId,
    int? ArchiveLocationId);

public interface IDocumentArchiveRepository
{
    /// <summary>Every archive event of a document, newest first.</summary>
    Task<IReadOnlyList<DocumentArchiveEvent>> ListByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    /// <summary>
    /// A page of archived documents the caller may view, each with its latest archive event. Access is
    /// part of the query, so paging counters never include a document the caller cannot see.
    /// <see cref="PageRequest.SortBy"/> accepts <c>archivedAt</c>, <c>title</c>, and <c>officialNumber</c>;
    /// the default is most recently archived first.
    /// </summary>
    Task<PagedList<ArchivedDocumentRow>> SearchAsync(
        ArchiveSearchFilter filter,
        PageRequest page,
        CancellationToken cancellationToken = default);
    void Add(DocumentArchiveEvent archiveEvent);
}

public interface ISavedSearchRepository
{
    Task<SavedSearch?> GetByIdAsync(Guid savedSearchId, CancellationToken cancellationToken = default);
    /// <summary>The user's saved searches ordered by name.</summary>
    Task<IReadOnlyList<SavedSearch>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SavedSearch?> GetDefaultAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(Guid userId, string name, Guid? excludingSavedSearchId = null, CancellationToken cancellationToken = default);
    Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(SavedSearch savedSearch);
    void Remove(SavedSearch savedSearch);
}

public interface IDocumentRelationRepository
{
    Task<DocumentRelation?> GetByIdAsync(Guid relationId, CancellationToken cancellationToken = default);
    /// <summary>Every relation that has the document as its source or its target.</summary>
    Task<IReadOnlyList<DocumentRelation>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    /// <summary>The relation type with its symmetry and cycle flags, or null when the id is unknown.</summary>
    Task<DocumentRelationType?> GetTypeByIdAsync(int relationTypeId, CancellationToken cancellationToken = default);
    /// <summary>The relation types with the given ids, for labelling a set of relations.</summary>
    Task<IReadOnlyList<DocumentRelationType>> ListTypesAsync(IReadOnlyCollection<int> relationTypeIds, CancellationToken cancellationToken = default);
    /// <summary>
    /// True when the exact (source, target, type) row exists. Callers pass endpoints already normalized
    /// by <see cref="DocumentRelationType.NormalizeEndpoints"/>.
    /// </summary>
    Task<bool> ExistsAsync(Guid sourceDocumentId, Guid targetDocumentId, int relationTypeId, CancellationToken cancellationToken = default);
    /// <summary>
    /// True when adding <c>source → target</c> of the given type would close a loop, that is, when the
    /// target already reaches the source through relations of that type. Serializes concurrent callers
    /// on the type so two opposite links cannot both pass the check.
    /// </summary>
    Task<bool> WouldCreateCycleAsync(int relationTypeId, Guid sourceDocumentId, Guid targetDocumentId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Tracked relations of this type that leave the document, newest first. Unification stores one
    /// <c>JoinedInto</c> edge from the joined file to the principal file and removes it on cancel.
    /// </summary>
    Task<IReadOnlyList<DocumentRelation>> ListOutgoingAsync(
        Guid sourceDocumentId,
        int relationTypeId,
        CancellationToken cancellationToken = default);
    /// <summary>Every relation whose source or target is one of the given documents.</summary>
    Task<IReadOnlyList<DocumentRelation>> ListTouchingAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken = default);
    /// <summary>The relation history of a document, newest first, including relations since removed.</summary>
    Task<PagedList<DocumentRelationEvent>> ListEventsByDocumentAsync(Guid documentId, PageRequest page, CancellationToken cancellationToken = default);
    void Add(DocumentRelation relation);
    void Add(DocumentRelationEvent relationEvent);
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

    /// <summary>
    /// The user's own inbox rows; dismissed rows are never listed. Newest first unless
    /// <see cref="PageRequest.SortBy"/> is one of deliveredAt, priority, type or isRead;
    /// <see cref="PageRequest.Search"/> matches the Arabic title and body.
    /// </summary>
    Task<PagedList<NotificationInboxItem>> GetInboxAsync(
        Guid userId, bool unreadOnly, int? typeId, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>The recipients (id + user) of a notification, for the realtime fan-out.</summary>
    Task<IReadOnlyList<NotificationRecipient>> GetRecipientsAsync(
        Guid notificationId, CancellationToken cancellationToken = default);

    Task<NotificationOutbox?> GetOutboxByIdAsync(Guid outboxId, CancellationToken cancellationToken = default);

    Task<NotificationType?> GetTypeAsync(int notificationTypeId, CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>The unread, non-dismissed rows of the user grouped by notification type (types with none are omitted).</summary>
    Task<IReadOnlyList<UnreadTypeCount>> CountUnreadByTypeAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>A recipient row, only when it belongs to <paramref name="userId"/> (tracked for update).</summary>
    Task<NotificationRecipient?> GetRecipientAsync(
        Guid recipientId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Marks every unread, non-dismissed row of the user as read; returns how many changed.</summary>
    Task<int> MarkAllReadAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>Outbox rows that are pending or failed and due for a (re)try, oldest first.</summary>
    Task<IReadOnlyList<NotificationOutbox>> GetDueOutboxAsync(
        DateTime utcNow, int batchSize, CancellationToken cancellationToken = default);

    /// <summary>Deletes read/dismissed recipient rows delivered before the cutoff, and their emptied notifications.</summary>
    Task<int> PurgeRecipientsAsync(DateTime deliveredBefore, CancellationToken cancellationToken = default);

    /// <summary>Deletes SENT outbox rows processed before the cutoff; dead letters are never touched.</summary>
    Task<int> PurgeSentOutboxAsync(DateTime processedBefore, CancellationToken cancellationToken = default);
}

/// <summary>How many unread notifications of one type a user has.</summary>
public sealed record UnreadTypeCount(int TypeId, int Count);

/// <summary>One inbox row: the recipient state joined with its notification content.</summary>
public sealed record NotificationInboxItem(
    Guid RecipientId,
    Guid NotificationId,
    int TypeId,
    string TitleAr,
    string BodyAr,
    int PriorityId,
    Guid? RelatedDocumentId,
    Guid? RelatedMovementId,
    string? ActionUrl,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);

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

    /// <summary>
    /// Makes an active entry the catalog's only default (clearing the previous one), or removes its default
    /// mark. Returns null when the entry does not exist.
    /// </summary>
    Task<Result<LookupEntity?>> SetDefaultStateAsync(
        string lookupKey,
        int id,
        bool isDefault,
        CancellationToken cancellationToken = default);

    /// <summary>True when any table holds a foreign key to this entry, so it must not be deactivated.</summary>
    Task<bool> IsInUseAsync(string lookupKey, int id, CancellationToken cancellationToken = default);
}
