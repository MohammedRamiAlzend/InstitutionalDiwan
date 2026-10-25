using InstitutionalDiwan.Application.Features.Audit;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Infrastructure.Time;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class AuditHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 5, 1, 10, 30, 0, DateTimeKind.Utc);

    private static (FakeAuditLogRepository Audit, FakeUserRepository Users, User User) Setup()
    {
        var user = User.Create("auditor", "fake:hash", Guid.NewGuid()).Value;
        var users = new FakeUserRepository();
        users.Add(user);
        return (new FakeAuditLogRepository(), users, user);
    }

    private static AuditLogAppender Appender(
        FakeAuditLogRepository audit,
        FakeUnitOfWork unitOfWork) =>
        new(audit, unitOfWork, new FakeRequestContext(), new DateTimeProvider(TestClock.Instance));

    private static AuditLog AppendRaw(FakeAuditLogRepository audit, Guid actor, string module = "DOCUMENTS")
    {
        var previous = audit.Logs.OrderByDescending(log => log.AuditLogId).FirstOrDefault()?.EventHash
            ?? AuditLog.GenesisHash;

        var result = AuditLog.Record(
            module,
            AuditActionType.CreateId,
            AuditEntityType.DocumentId,
            Guid.NewGuid().ToString(),
            previous,
            actor,
            occurredAtUtc: FixedNow.AddMinutes(audit.Logs.Count));
        Assert.True(result.IsSuccess);

        audit.Add(result.Value);
        return result.Value;
    }

    [Fact]
    public async Task GetAuditLogs_ReturnsPageAndAppendsASelfAuditEntry()
    {
        var (audit, users, user) = Setup();
        AppendRaw(audit, user.UserId);
        var unitOfWork = new FakeUnitOfWork();

        var handler = new GetAuditLogsQueryHandler(
            audit, users, Appender(audit, unitOfWork), unitOfWork);

        var result = await handler.Handle(
            new GetAuditLogsQuery(user.UserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Single(result.Value.Items);
        Assert.Equal(2, audit.Logs.Count);

        var selfAudit = audit.Logs[^1];
        Assert.Equal("AUDIT", selfAudit.ModuleName);
        Assert.Equal(AuditActionType.ViewId, selfAudit.ActionTypeId);
        Assert.Equal(AuditEntityType.AuditLogId, selfAudit.EntityTypeId);
        Assert.Equal("AUDIT_LOGS", selfAudit.EntityId);
        Assert.Equal(user.UserId, selfAudit.ActorUserId);
        Assert.Equal("test-correlation", selfAudit.CorrelationId);
        Assert.Equal("10.0.0.15", selfAudit.IpAddress);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task GetAuditLogs_WhenUserMissing_ReturnsNotFound()
    {
        var (audit, users, _) = Setup();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new GetAuditLogsQueryHandler(
            audit, users, Appender(audit, unitOfWork), unitOfWork);

        var result = await handler.Handle(
            new GetAuditLogsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserNotFound.Code, result.Error.Code);
        Assert.Empty(audit.Logs);
    }

    [Fact]
    public async Task CreateCheckpoint_SealsEntriesAfterThePreviousCheckpoint()
    {
        var (audit, users, user) = Setup();
        AppendRaw(audit, user.UserId);
        AppendRaw(audit, user.UserId);
        var unitOfWork = new FakeUnitOfWork();

        var handler = new CreateAuditCheckpointCommandHandler(
            audit, users, Appender(audit, unitOfWork), unitOfWork);

        var result = await handler.Handle(
            new CreateAuditCheckpointCommand(user.UserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.FromAuditLogId);
        Assert.Equal(2, result.Value.ToAuditLogId);
        Assert.Equal(2, result.Value.RecordCount);
        Assert.Equal(audit.Logs[1].EventHash, result.Value.TerminalHash);
        Assert.Single(audit.Checkpoints);
        Assert.Equal(3, audit.Logs.Count);
        Assert.Equal(AuditActionType.CreateId, audit.Logs[^1].ActionTypeId);
    }

    [Fact]
    public async Task CreateCheckpoint_WhenRangeHasNoNewEntries_ReturnsConflict()
    {
        var (audit, users, user) = Setup();
        AppendRaw(audit, user.UserId);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateAuditCheckpointCommandHandler(
            audit, users, Appender(audit, unitOfWork), unitOfWork);

        var result = await handler.Handle(
            new CreateAuditCheckpointCommand(user.UserId, ToAuditLogId: 0),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Audit.NothingToCheckpoint.Code, result.Error.Code);
        Assert.Empty(audit.Checkpoints);
    }

    [Fact]
    public async Task CreateCheckpoint_WhenChainIsBroken_RefusesToSeal()
    {
        var (audit, users, user) = Setup();
        AppendRaw(audit, user.UserId);
        AppendRaw(audit, user.UserId);
        audit.TamperModuleName(1, "TAMPERED");

        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateAuditCheckpointCommandHandler(
            audit, users, Appender(audit, unitOfWork), unitOfWork);

        var result = await handler.Handle(
            new CreateAuditCheckpointCommand(user.UserId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(audit.Checkpoints);
    }

    [Fact]
    public async Task CreateCheckpoint_WhenThereAreNoAuditLogs_ReturnsNotFound()
    {
        var (audit, users, user) = Setup();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateAuditCheckpointCommandHandler(
            audit, users, Appender(audit, unitOfWork), unitOfWork);

        var result = await handler.Handle(
            new CreateAuditCheckpointCommand(user.UserId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Audit.NoAuditLogs.Code, result.Error.Code);
        Assert.Empty(audit.Checkpoints);
    }

    [Fact]
    public async Task RunIntegrityCheck_WhenThereAreNoAuditLogs_ReturnsNotFound()
    {
        var (audit, users, user) = Setup();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RunAuditIntegrityCheckCommandHandler(
            audit, users, Appender(audit, unitOfWork), unitOfWork);

        var result = await handler.Handle(
            new RunAuditIntegrityCheckCommand(user.UserId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Audit.NoAuditLogs.Code, result.Error.Code);
        Assert.Empty(audit.IntegrityChecks);
    }

    [Fact]
    public async Task RunIntegrityCheck_OnUntouchedChain_RecordsPass()
    {
        var (audit, users, user) = Setup();
        AppendRaw(audit, user.UserId);
        AppendRaw(audit, user.UserId);
        var unitOfWork = new FakeUnitOfWork();

        var handler = new RunAuditIntegrityCheckCommandHandler(
            audit, users, Appender(audit, unitOfWork), unitOfWork);

        var result = await handler.Handle(
            new RunAuditIntegrityCheckCommand(user.UserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsValid);
        Assert.Null(result.Value.FirstCorruptedLogId);
        Assert.Single(audit.IntegrityChecks);
        Assert.Equal(1, result.Value.StartAuditLogId);
        Assert.Equal(2, result.Value.EndAuditLogId);
    }

    [Fact]
    public async Task RunIntegrityCheck_AfterTampering_RecordsFirstCorruptedEntry()
    {
        var (audit, users, user) = Setup();
        AppendRaw(audit, user.UserId);
        AppendRaw(audit, user.UserId);
        audit.TamperModuleName(1, "TAMPERED");

        var unitOfWork = new FakeUnitOfWork();
        var handler = new RunAuditIntegrityCheckCommandHandler(
            audit, users, Appender(audit, unitOfWork), unitOfWork);

        var result = await handler.Handle(
            new RunAuditIntegrityCheckCommand(user.UserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsValid);
        Assert.Equal(1, result.Value.FirstCorruptedLogId);
        Assert.Contains("HashMismatch", result.Value.FailureDetails);
        Assert.Single(audit.IntegrityChecks);
    }

    [Fact]
    public async Task RunIntegrityCheck_WithStartAfterFirstEntry_UsesPreviousEventHash()
    {
        var (audit, users, user) = Setup();
        AppendRaw(audit, user.UserId);
        AppendRaw(audit, user.UserId);
        var unitOfWork = new FakeUnitOfWork();

        var handler = new RunAuditIntegrityCheckCommandHandler(
            audit, users, Appender(audit, unitOfWork), unitOfWork);

        var result = await handler.Handle(
            new RunAuditIntegrityCheckCommand(user.UserId, StartAuditLogId: 2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsValid);
        Assert.Equal(2, result.Value.StartAuditLogId);
    }

    [Fact]
    public async Task GetIntegrityReports_ReturnsNewestFirst()
    {
        var (audit, users, user) = Setup();
        AppendRaw(audit, user.UserId);
        AppendRaw(audit, user.UserId);
        var unitOfWork = new FakeUnitOfWork();

        var runner = new RunAuditIntegrityCheckCommandHandler(
            audit, users, Appender(audit, unitOfWork), unitOfWork);
        await runner.Handle(new RunAuditIntegrityCheckCommand(user.UserId), CancellationToken.None);

        var handler = new GetAuditIntegrityReportsQueryHandler(audit);
        var result = await handler.Handle(
            new GetAuditIntegrityReportsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.True(result.Value.Items.Single().IsValid);
    }
}
