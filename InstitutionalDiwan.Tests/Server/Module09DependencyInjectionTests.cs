using InstitutionalDiwan.Application;
using InstitutionalDiwan.Application.Abstractions.Audit;
using InstitutionalDiwan.Application.Abstractions.Context;
using InstitutionalDiwan.Application.Abstractions.Persistence;
using InstitutionalDiwan.Application.Abstractions.Time;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Audit;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Infrastructure;
using InstitutionalDiwan.Infrastructure.BackgroundJobs;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Interceptors;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InstitutionalDiwan.Tests.Server;

/// <summary>
/// Verifies that Module 09 is wired through the real composition root: the repository, appender,
/// clock/request-context abstractions, the EF Core interceptor, the scheduled jobs, the permission
/// policies, and every MediatR handler resolve from the same container the server builds.
/// </summary>
public sealed class Module09DependencyInjectionTests
{
    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\MSSQLLocalDB;Database=InstitutionalDiwanTests;Trusted_Connection=True;TrustServerCertificate=True",
                ["Jwt:Issuer"] = "InstitutionalDiwan",
                ["Jwt:Audience"] = "InstitutionalDiwan",
                ["Jwt:SecretKey"] = "module-09-injection-test-signing-key-0123456789",
                ["Jwt:AccessTokenExpirationMinutes"] = "60"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddInstitutionalAuthentication(configuration);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void Audit_services_resolve_from_the_composition_root()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        Assert.NotNull(services.GetRequiredService<IAuditLogRepository>());
        Assert.NotNull(services.GetRequiredService<AuditLogAppender>());
        Assert.NotNull(services.GetRequiredService<IAuditActionRecorder>());
        Assert.NotNull(services.GetRequiredService<IRequestContext>());
        Assert.NotNull(services.GetRequiredService<ITransactionManager>());
        Assert.NotNull(services.GetRequiredService<IDateTimeProvider>());
        Assert.NotNull(services.GetRequiredService<TamperEvidentAuditInterceptor>());
    }

    [Fact]
    public void Audit_interceptor_is_attached_to_the_context_and_jobs_are_hosted()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        var options = services.GetRequiredService<DbContextOptions<DiwanDbContext>>();
        var interceptors = options.FindExtension<CoreOptionsExtension>()?.Interceptors ?? [];

        Assert.Contains(interceptors, interceptor => interceptor is TamperEvidentAuditInterceptor);

        var hostedServices = services.GetServices<IHostedService>().ToList();
        Assert.Contains(hostedServices, job => job is AuditChainIntegrityVerificationJob);
        Assert.Contains(hostedServices, job => job is DailyAuditCheckpointJob);
    }

    [Fact]
    public async Task Audit_policies_and_handlers_are_injected()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        var policyProvider = services.GetRequiredService<IAuthorizationPolicyProvider>();
        Assert.NotNull(await policyProvider.GetPolicyAsync(Permissions.AuditView));
        Assert.NotNull(await policyProvider.GetPolicyAsync(Permissions.AuditManage));

        Assert.NotNull(services.GetRequiredService<
            IRequestHandler<GetAuditLogsQuery, Result<PagedResult<AuditLogResponse>>>>());
        Assert.NotNull(services.GetRequiredService<
            IRequestHandler<CreateAuditCheckpointCommand, Result<AuditCheckpointResponse>>>());
        Assert.NotNull(services.GetRequiredService<
            IRequestHandler<RunAuditIntegrityCheckCommand, Result<AuditIntegrityCheckResponse>>>());
        Assert.NotNull(services.GetRequiredService<
            IRequestHandler<GetAuditIntegrityReportsQuery, Result<PagedResult<AuditIntegrityCheckResponse>>>>());
    }
}
