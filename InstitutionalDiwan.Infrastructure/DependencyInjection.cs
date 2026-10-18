using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Infrastructure.Authentication;
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
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDepartmentTypeRepository, DepartmentTypeRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentNumberRepository, DocumentNumberRepository>();
        services.AddScoped<INumberingRegistryRepository, NumberingRegistryRepository>();

        // Ordered, environment-aware seeders — executed at startup by SeederRunner.
        services.AddScoped<ISeeder, PermissionSeeder>();
        services.AddScoped<ISeeder, RoleSeeder>();
        services.AddScoped<ISeeder, UserSeeder>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtProvider, JwtProvider>();

        return services;
    }
}
