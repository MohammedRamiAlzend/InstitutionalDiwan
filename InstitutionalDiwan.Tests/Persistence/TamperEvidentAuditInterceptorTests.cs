using InstitutionalDiwan.Application.Abstractions.Context;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Infrastructure.Persistence.Interceptors;
using InstitutionalDiwan.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace InstitutionalDiwan.Tests.Persistence;

public sealed class TamperEvidentAuditInterceptorTests
{
    private static readonly Guid ActorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DepartmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static IHttpContextAccessor Accessor(bool authenticated = true)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.15");
        context.Items[RequestContextKeys.CorrelationId] = "correlation-1";

        if (authenticated)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ActorId.ToString()),
                new Claim("dept_id", DepartmentId.ToString())
            }, authenticationType: "Test"));
        }

        return new HttpContextAccessor { HttpContext = context };
    }

    [Fact]
    public async Task SaveChanges_AppendsADomainEventEntryWithRequestMetadata()
    {
        using var db = TestContextFactory.Create(
            new TamperEvidentAuditInterceptor(Accessor(), TestClock.Instance));
        TestContextFactory.SeedAuditLookups(db);

        db.DepartmentTypes.Add(DepartmentType.Create(5, "Central Diwan", "الديوان المركزي", 0).Value);
        var department = Department.Create("DIWAN", "الديوان", 5).Value;
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var log = await db.AuditLogs.SingleAsync();

        Assert.Equal("ORGANIZATION", log.ModuleName);
        Assert.Equal(AuditActionType.CreateId, log.ActionTypeId);
        Assert.Equal(AuditEntityType.DepartmentId, log.EntityTypeId);
        Assert.Equal(department.DepartmentId.ToString(), log.EntityId);
        Assert.Equal(ActorId, log.ActorUserId);
        Assert.Equal(DepartmentId, log.ActorDepartmentId);
        Assert.Equal("10.0.0.15", log.IpAddress);
        Assert.Equal("correlation-1", log.CorrelationId);
        Assert.Equal(AuditLog.GenesisHash, log.PreviousHash);
        Assert.True(log.VerifyIntegrity(AuditLog.GenesisHash).IsSuccess);
    }

    [Fact]
    public async Task SaveChanges_ChainsMultipleEntriesInOrder()
    {
        using var db = TestContextFactory.Create(
            new TamperEvidentAuditInterceptor(Accessor(), TestClock.Instance));
        TestContextFactory.SeedAuditLookups(db);

        db.DepartmentTypes.Add(DepartmentType.Create(5, "Central Diwan", "الديوان المركزي", 0).Value);
        db.Departments.Add(Department.Create("DIWAN", "الديوان", 5).Value);
        await db.SaveChangesAsync();

        db.Roles.Add(Role.Create("SUPERVISOR", "مشرف").Value);
        await db.SaveChangesAsync();

        var logs = await db.AuditLogs.OrderBy(log => log.AuditLogId).ToListAsync();

        Assert.Equal(2, logs.Count);
        Assert.Equal(logs[0].EventHash, logs[1].PreviousHash);
        Assert.Equal("IDENTITY", logs[1].ModuleName);
        Assert.Equal(AuditEntityType.RoleId, logs[1].EntityTypeId);
        Assert.True(logs[1].VerifyIntegrity(logs[0].EventHash).IsSuccess);
    }

    [Fact]
    public async Task SaveChanges_DoesNotPersistPasswordOrOtherSensitiveFields()
    {
        using var db = TestContextFactory.Create(
            new TamperEvidentAuditInterceptor(Accessor(), TestClock.Instance));
        TestContextFactory.SeedAuditLookups(db);

        db.DepartmentTypes.Add(DepartmentType.Create(5, "Central Diwan", "الديوان المركزي", 0).Value);
        var department = Department.Create("DIWAN", "الديوان", 5).Value;
        db.Departments.Add(department);
        db.UserScopeTypes.Add(UserScopeType.Create(1, "Department", "دائرة").Value);
        await db.SaveChangesAsync();

        Assert.True(await db.UserScopeTypes.AnyAsync(type => type.Id == 1));
        Assert.True(await db.Departments.AnyAsync(item => item.DepartmentId == department.DepartmentId));

        db.Users.Add(User.Create("auditor", "super-secret-password-hash", department.DepartmentId).Value);
        await db.SaveChangesAsync();

        var log = await db.AuditLogs.OrderBy(item => item.AuditLogId).LastAsync();

        Assert.Equal("IDENTITY", log.ModuleName);
        Assert.Equal(AuditEntityType.UserId, log.EntityTypeId);
        Assert.NotNull(log.NewValuesJson);
        Assert.DoesNotContain("super-secret-password-hash", log.NewValuesJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", log.NewValuesJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveChanges_OutsideAnHttpRequest_DoesNotWriteAuditRows()
    {
        var accessor = Accessor();
        using var db = TestContextFactory.Create(
            new TamperEvidentAuditInterceptor(accessor, TestClock.Instance));
        TestContextFactory.SeedAuditLookups(db);

        accessor.HttpContext = null;
        db.Roles.Add(Role.Create("BACKGROUND", "خلفي").Value);
        await db.SaveChangesAsync();

        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task SaveChanges_DoesNotAuditAuditLogsThemselves()
    {
        using var db = TestContextFactory.Create(
            new TamperEvidentAuditInterceptor(Accessor(), TestClock.Instance));
        TestContextFactory.SeedAuditLookups(db);

        db.DepartmentTypes.Add(DepartmentType.Create(5, "Central Diwan", "الديوان المركزي", 0).Value);
        db.Departments.Add(Department.Create("DIWAN", "الديوان", 5).Value);
        await db.SaveChangesAsync();

        var latest = await db.AuditLogs
            .OrderByDescending(log => log.AuditLogId)
            .FirstAsync();

        var seeded = AuditLog.Record(
            "AUDIT",
            AuditActionType.ViewId,
            AuditEntityType.AuditLogId,
            "AUDIT_LOGS",
            latest.EventHash,
            actorUserId: ActorId).Value;
        db.AuditLogs.Add(seeded);
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.AuditLogs.CountAsync());
    }
}
