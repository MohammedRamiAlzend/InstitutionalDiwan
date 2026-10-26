using InstitutionalDiwan.Application.Abstractions.Audit;
using InstitutionalDiwan.Application.Abstractions.Context;
using InstitutionalDiwan.Application.Abstractions.Time;
using InstitutionalDiwan.Application.Features.Audit;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Infrastructure.Context;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Interceptors;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Security.Claims;

namespace InstitutionalDiwan.Tests.Persistence;

public sealed class AuditActionRecorderTests
{
    private static readonly Guid ActorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DepartmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task RecordAsync_WritesAChainedRowFromAnIsolatedScope()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var accessor = new HttpContextAccessor();
        var interceptor = new TamperEvidentAuditInterceptor(accessor, TestClock.Instance);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(accessor);
        services.AddSingleton<IClock>(TestClock.Instance);
        services.AddSingleton<IDateTimeProvider, FixedDateTimeProvider>();
        services.AddSingleton<ILogger<AuditActionRecorder>>(
            NullLogger<AuditActionRecorder>.Instance);
        services.AddDbContext<DiwanDbContext>(options =>
            options.UseSqlite(connection).AddInterceptors(interceptor));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<DiwanDbContext>());
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IRequestContext, HttpRequestContext>();
        services.AddScoped<AuditLogAppender>();
        services.AddSingleton<IAuditActionRecorder, AuditActionRecorder>();

        await using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DiwanDbContext>();
            await db.Database.EnsureCreatedAsync();

            for (var id = 1; id <= 8; id++)
                db.AuditActionTypes.Add(AuditActionType.Create(id, $"Action {id}", $"إجراء {id}").Value);

            for (var id = 1; id <= 13; id++)
                db.AuditEntityTypes.Add(AuditEntityType.Create(id, $"Entity {id}", $"كيان {id}").Value);

            await db.SaveChangesAsync();
        }

        accessor.HttpContext = AuthenticatedContext();

        // A pending business change in a request-scoped context must never be flushed by the
        // recorder. Recording uses its own scope and therefore its own DbContext.
        using var requestScope = provider.CreateScope();
        var requestDb = requestScope.ServiceProvider.GetRequiredService<DiwanDbContext>();
        requestDb.Roles.Add(Role.Create("PENDING", "معلق").Value);

        var recorder = provider.GetRequiredService<IAuditActionRecorder>();

        await recorder.RecordAsync(new AuditActionRecord(
            "CORRESPONDENCE",
            AuditActionType.ViewId,
            AuditEntityType.OperationId,
            "Documents.List",
            ActorId,
            DepartmentId,
            "{\"statusCode\":200}"));

        using var verifyScope = provider.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DiwanDbContext>();

        Assert.Empty(await verifyDb.Roles.ToListAsync());

        var log = await verifyDb.AuditLogs.SingleAsync();
        Assert.Equal("CORRESPONDENCE", log.ModuleName);
        Assert.Equal(AuditActionType.ViewId, log.ActionTypeId);
        Assert.Equal(AuditEntityType.OperationId, log.EntityTypeId);
        Assert.Equal("Documents.List", log.EntityId);
        Assert.Equal(ActorId, log.ActorUserId);
        Assert.Equal(DepartmentId, log.ActorDepartmentId);
        Assert.Equal("correlation-1", log.CorrelationId);
        Assert.Equal("10.0.0.15", log.IpAddress);
        Assert.Equal(AuditLog.GenesisHash, log.PreviousHash);
        Assert.True(log.VerifyIntegrity(AuditLog.GenesisHash).IsSuccess);
    }

    private static HttpContext AuthenticatedContext()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.15");
        context.Items[RequestContextKeys.CorrelationId] = "correlation-1";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, ActorId.ToString()),
            new Claim("dept_id", DepartmentId.ToString())
        }, authenticationType: "Test"));
        return context;
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => TestClock.Instance.UtcNow;

        public DateTime ToInstitutionLocal(DateTime utc) => utc;

        public int NumberingYear(DateTime utc) => utc.Year;
    }
}
