using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Infrastructure.Authentication;
using InstitutionalDiwan.Infrastructure.Authorization;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InstitutionalDiwan.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers persistence, repositories, and the authentication services.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<DiwanDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=InstitutionalDiwan;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");
        });
        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<DiwanDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IDocumentAccessGrantRepository, DocumentAccessGrantRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDepartmentTypeRepository, DepartmentTypeRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
        services.AddScoped<IDocumentNumberRepository, DocumentNumberRepository>();
        services.AddScoped<INumberingRegistryRepository, NumberingRegistryRepository>();
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<IInstitutionSettingRepository, InstitutionSettingRepository>();
        services.AddScoped<IExternalOrganizationRepository, ExternalOrganizationRepository>();
        services.AddScoped<IDocumentMovementRepository, DocumentMovementRepository>();

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
        services.AddScoped<ISeeder, NumberingResetPolicySeeder>();
        services.AddScoped<ISeeder, PermissionSeeder>();
        services.AddScoped<ISeeder, RoleSeeder>();
        services.AddScoped<ISeeder, UserSeeder>();
        services.AddScoped<ISeeder, InstitutionSettingSeeder>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtProvider, JwtProvider>();

        return services;
    }
}
