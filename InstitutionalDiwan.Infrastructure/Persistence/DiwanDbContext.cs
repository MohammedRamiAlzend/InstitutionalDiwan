using Microsoft.EntityFrameworkCore;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Entities.Relations;

namespace InstitutionalDiwan.Infrastructure.Persistence;

public sealed class DiwanDbContext : DbContext, IUnitOfWork
{
    public DiwanDbContext(DbContextOptions<DiwanDbContext> options)
        : base(options)
    {
    }

    // Module 01: Organization
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<DepartmentType> DepartmentTypes => Set<DepartmentType>();
    public DbSet<InstitutionSetting> InstitutionSettings => Set<InstitutionSetting>();

    // Module 02: Identity & Access
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<UserDepartmentAssignment> UserDepartmentAssignments => Set<UserDepartmentAssignment>();
    public DbSet<DocumentAccessGrant> DocumentAccessGrants => Set<DocumentAccessGrant>();

    // Module 03: Correspondence
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<DocumentStatusType> DocumentStatusTypes => Set<DocumentStatusType>();
    public DbSet<DocumentParticipant> DocumentParticipants => Set<DocumentParticipant>();
    public DbSet<DocumentStatusHistory> DocumentStatusHistories => Set<DocumentStatusHistory>();
    public DbSet<ExternalOrganization> ExternalOrganizations => Set<ExternalOrganization>();

    // Module 04: Official Numbering
    public DbSet<DocumentNumber> DocumentNumbers => Set<DocumentNumber>();
    public DbSet<NumberingRegistry> NumberingRegistries => Set<NumberingRegistry>();
    public DbSet<NumberingCounter> NumberingCounters => Set<NumberingCounter>();
    public DbSet<DocumentNumberHistory> DocumentNumberHistories => Set<DocumentNumberHistory>();

    // Module 05: Movements
    public DbSet<DocumentMovement> DocumentMovements => Set<DocumentMovement>();
    public DbSet<DocumentMovementStep> DocumentMovementSteps => Set<DocumentMovementStep>();
    public DbSet<DocumentMovementEvent> DocumentMovementEvents => Set<DocumentMovementEvent>();

    // Module 06: Attachments
    public DbSet<DocumentFile> DocumentFiles => Set<DocumentFile>();
    public DbSet<DocumentFileVersion> DocumentFileVersions => Set<DocumentFileVersion>();
    public DbSet<DocumentFileType> DocumentFileTypes => Set<DocumentFileType>();
    public DbSet<FileStorageProvider> FileStorageProviders => Set<FileStorageProvider>();

    // Module 07: Archiving
    public DbSet<DocumentArchiveEvent> DocumentArchiveEvents => Set<DocumentArchiveEvent>();
    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();

    // Module 08: Relations
    public DbSet<DocumentRelation> DocumentRelations => Set<DocumentRelation>();
    public DbSet<DocumentRelationType> DocumentRelationTypes => Set<DocumentRelationType>();
    public DbSet<DocumentRelationEvent> DocumentRelationEvents => Set<DocumentRelationEvent>();

    // Module 09: Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AuditCheckpoint> AuditCheckpoints => Set<AuditCheckpoint>();
    public DbSet<AuditIntegrityCheck> AuditIntegrityChecks => Set<AuditIntegrityCheck>();

    // Module 10: Notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
    public DbSet<NotificationOutbox> NotificationOutboxes => Set<NotificationOutbox>();
    public DbSet<NotificationType> NotificationTypes => Set<NotificationType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DiwanDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Design-time fallback for EF CLI tools that construct the context without configured
        // options, so migrations target SQL Server. Skipped when options are already configured
        // (dependency injection at runtime, or alternative providers such as InMemory in tests).
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=InstitutionalDiwan;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");
        }

        base.OnConfiguring(optionsBuilder);
    }

}
