using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Context;
using InstitutionalDiwan.Application.Abstractions.Persistence;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Application.Abstractions.Time;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Infrastructure.Authentication;
using InstitutionalDiwan.Infrastructure.Authorization;
using InstitutionalDiwan.Infrastructure.BackgroundJobs;
using InstitutionalDiwan.Infrastructure.Context;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Interceptors;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using InstitutionalDiwan.Infrastructure.Persistence.Transactions;
using InstitutionalDiwan.Infrastructure.Storage;
using InstitutionalDiwan.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InstitutionalDiwan.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers persistence, repositories, and the authentication services.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<DiwanDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=InstitutionalDiwan;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

            // The interceptor appends the audit trail from the same SaveChanges call that writes
            // the business change, so the two rows share one transaction.
            options.AddInterceptors(serviceProvider.GetRequiredService<TamperEvidentAuditInterceptor>());
        });
        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<DiwanDbContext>());

        // One ambient source of "now" for the whole application.
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<TamperEvidentAuditInterceptor>();

        // Request metadata and one explicit transaction scope per command.
        services.AddScoped<IRequestContext, HttpRequestContext>();
        services.AddScoped<ITransactionManager, EfTransactionManager>();

        // Scheduled audit maintenance. Both jobs are idempotent and no-op when there is nothing
        // new to seal or no entries to verify.
        services.Configure<AuditJobsOptions>(configuration.GetSection(AuditJobsOptions.SectionName));
        services.AddHostedService<AuditChainIntegrityVerificationJob>();
        services.AddHostedService<DailyAuditCheckpointJob>();

        // Notification outbox delivery and retention.
        services.Configure<NotificationJobsOptions>(configuration.GetSection(NotificationJobsOptions.SectionName));
        services.AddHostedService<OutboxProcessorJob>();
        services.AddHostedService<NotificationRetentionJob>();
        services.AddSingleton<Diagnostics.SystemErrorQueue>();
        services.AddSingleton<Application.Features.Diagnostics.SystemErrors.ISystemErrorQueue>(
            provider => provider.GetRequiredService<Diagnostics.SystemErrorQueue>());
        services.AddHostedService<Diagnostics.SystemErrorWriterJob>();
        services.AddSingleton<Application.Features.Diagnostics.SystemErrors.ISystemErrorExcelWriter, Diagnostics.SystemErrorExcelWriter>();
        services.AddSingleton<Application.Features.Exports.ITableExcelWriter, Exports.TableExcelWriter>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IDocumentAccessGrantRepository, DocumentAccessGrantRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDepartmentTypeRepository, DepartmentTypeRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
        services.AddScoped<IDocumentFieldRepository, DocumentFieldRepository>();
        services.AddScoped<ISystemErrorRepository, Diagnostics.SystemErrorRepository>();
        services.AddScoped<IDocumentTypeSchemaVersionRepository, DocumentTypeSchemaVersionRepository>();
        services.AddScoped<IDocumentNumberRepository, DocumentNumberRepository>();
        services.AddScoped<INumberingRegistryRepository, NumberingRegistryRepository>();
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<IInstitutionSettingRepository, InstitutionSettingRepository>();
        services.AddScoped<IExternalOrganizationRepository, ExternalOrganizationRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IDocumentMovementRepository, DocumentMovementRepository>();
        services.AddScoped<IDocumentFileRepository, DocumentFileRepository>();
        services.AddScoped<IDocumentFileTypeRepository, DocumentFileTypeRepository>();
        services.AddScoped<IFileStorageProviderRepository, FileStorageProviderRepository>();
        services.AddScoped<IDocumentArchiveRepository, DocumentArchiveRepository>();
        services.AddScoped<ISavedSearchRepository, SavedSearchRepository>();
        services.AddScoped<IDocumentRelationRepository, DocumentRelationRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<InstitutionalDiwan.Application.Abstractions.Notifications.INotificationRecipientDirectory, NotificationRecipientDirectory>();

        // Module 06 physical storage: local file system + magic-byte validation, both consumed by
        // the Attachments handlers through Application abstractions.
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddSingleton<IFileSignatureValidator, FileSignatureValidator>();

        // Ambient request identity and the three-tier access evaluators (RBAC → scope → document).
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IDepartmentScopeEvaluator, DepartmentScopeEvaluator>();
        services.AddScoped<IDocumentAccessEvaluator, DocumentAccessEvaluator>();

        // Ordered, environment-aware seeders — executed at startup by SeederRunner.
        services.AddScoped<ISeeder, UserScopeTypeSeeder>();
        services.AddScoped<ISeeder, AssignmentTypeSeeder>();
        services.AddScoped<ISeeder, AccessLevelSeeder>();
        services.AddScoped<ISeeder, ParticipantRoleSeeder>();
        services.AddScoped<ISeeder, ParticipantTargetTypeSeeder>();
        services.AddScoped<ISeeder, OrganizationTypeSeeder>();
        services.AddScoped<ISeeder, DocumentPersonRoleSeeder>();
        services.AddScoped<ISeeder, GenderSeeder>();
        services.AddScoped<ISeeder, MovementTypeSeeder>();
        services.AddScoped<ISeeder, MovementStatusSeeder>();
        services.AddScoped<ISeeder, MovementStepStatusSeeder>();
        services.AddScoped<ISeeder, MovementEventTypeSeeder>();
        services.AddScoped<ISeeder, StorageProviderTypeSeeder>();
        services.AddScoped<ISeeder, DocumentFileTypeSeeder>();
        services.AddScoped<ISeeder, FileStorageProviderSeeder>();
        services.AddScoped<ISeeder, DocumentRelationTypeSeeder>();
        services.AddScoped<ISeeder, RelationActionTypeSeeder>();
        services.AddScoped<ISeeder, ArchiveEventTypeSeeder>();
        services.AddScoped<ISeeder, ArchiveLocationSeeder>();
        services.AddScoped<ISeeder, AuditActionTypeSeeder>();
        services.AddScoped<ISeeder, AuditEntityTypeSeeder>();
        services.AddScoped<ISeeder, NotificationTypeSeeder>();
        services.AddScoped<ISeeder, NotificationPrioritySeeder>();
        services.AddScoped<ISeeder, NotificationChannelSeeder>();
        services.AddScoped<ISeeder, NotificationOutboxStatusSeeder>();
        services.AddScoped<ISeeder, NumberingModeSeeder>();
        services.AddScoped<ISeeder, DepartmentTypeSeeder>();
        services.AddScoped<ISeeder, DocumentDirectionSeeder>();
        services.AddScoped<ISeeder, DocumentTypeSeeder>();
        services.AddScoped<ISeeder, DocumentStatusTypeSeeder>();
        services.AddScoped<ISeeder, DocumentConfidentialityLevelSeeder>();
        services.AddScoped<ISeeder, DocumentPriorityLevelSeeder>();
        services.AddScoped<ISeeder, DocumentNumberStatusSeeder>();
        services.AddScoped<ISeeder, DocumentNumberEventTypeSeeder>();
        services.AddScoped<ISeeder, DocumentNumberScopeSeeder>();
        services.AddScoped<ISeeder, NumberingResetPolicySeeder>();
        services.AddScoped<ISeeder, PermissionSeeder>();
        services.AddScoped<ISeeder, RoleSeeder>();
        services.AddScoped<ISeeder, UserSeeder>();
        services.AddScoped<ISeeder, InstitutionSettingSeeder>();
        services.AddScoped<ISeeder, DepartmentSeeder>();
        services.AddScoped<ISeeder, EmployeeSeeder>();
        services.AddScoped<ISeeder, StandardDepartmentSeeder>();
        services.AddScoped<ISeeder, SuperAdminSeeder>();
        services.AddScoped<ISeeder, StandardAccountSeeder>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtProvider, JwtProvider>();

        return services;
    }
}
