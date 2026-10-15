

namespace InstitutionalDiwan.Domain.Repositories;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<Department?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> HasActiveChildrenAsync(Guid departmentId, CancellationToken cancellationToken = default);
    void Add(Department department);
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
    void Add(Role role);
}

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> ListRecentAsync(int limit, CancellationToken cancellationToken = default);
    void Add(Document document);
}

public interface INumberingRegistryRepository
{
    Task<NumberingRegistry?> GetByIdAsync(Guid registryId, CancellationToken cancellationToken = default);
    Task<NumberingRegistry?> GetActiveForScopeAsync(Guid? departmentId, string? documentTypeCode, string? direction, CancellationToken cancellationToken = default);
    void Add(NumberingRegistry registry);
}

public interface IDocumentNumberRepository
{
    Task<DocumentNumber?> GetByIdAsync(Guid documentNumberId, CancellationToken cancellationToken = default);
    Task<DocumentNumber?> GetActiveByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    void Add(DocumentNumber documentNumber);
}

public interface IDocumentMovementRepository
{
    Task<DocumentMovement?> GetByIdAsync(Guid movementId, CancellationToken cancellationToken = default);
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
