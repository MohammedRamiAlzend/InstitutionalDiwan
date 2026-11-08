using ClosedXML.Excel;
using InstitutionalDiwan.Application.Features.Diagnostics.SystemErrors;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Diagnostics;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Infrastructure.Diagnostics;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>Grouping, reopening, purge and Excel export of captured errors.</summary>
public sealed class SystemErrorStoreTests
{
    private static readonly DateTime Now = new(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);

    private static SystemErrorContext Context(string path = "/api/documents/7c9e3b42-8a61-4d52-9f17-2ab85c6e7934", DateTime? at = null) =>
        new(path, "GET", Guid.NewGuid(), "user", "agent", "trace", at ?? Now);

    private static SystemErrorOccurrence Rejection(DateTime? at = null, string path = "/api/documents/7c9e3b42-8a61-4d52-9f17-2ab85c6e7934") =>
        SystemErrorOccurrenceFactory.ForRejection("Correspondence.DocumentNotFound", "Not found.", 404, Context(path, at));

    private static Exception Thrown()
    {
        try
        {
            throw new InvalidOperationException("Order 12345 failed");
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    [Fact]
    public async Task RepeatedError_IsOneRow_AndCountsEveryOccurrence()
    {
        await using var db = TestContextFactory.Create();

        await SystemErrorStore.RecordAsync(db, Rejection(), CancellationToken.None);
        await SystemErrorStore.RecordAsync(db, Rejection(Now.AddMinutes(5), "/api/documents/11111111-2222-3333-4444-555555555555"), CancellationToken.None);
        db.ChangeTracker.Clear();

        var error = await db.SystemErrors.SingleAsync();
        Assert.Equal(2, error.OccurrenceCount);
        Assert.Equal(Now, error.FirstSeenAt);
        Assert.Equal(Now.AddMinutes(5), error.LastSeenAt);
        Assert.Equal(SystemErrorKind.ServerRejection, error.Kind);
    }

    [Fact]
    public async Task ResolvedError_SeenAgain_ReopensAsNew()
    {
        await using var db = TestContextFactory.Create();
        await SystemErrorStore.RecordAsync(db, Rejection(), CancellationToken.None);
        var error = await db.SystemErrors.SingleAsync();
        Assert.True(error.UpdateWork(SystemErrorStatus.Resolved, "أُصلح", Guid.NewGuid(), Now).IsSuccess);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await SystemErrorStore.RecordAsync(db, Rejection(Now.AddDays(1)), CancellationToken.None);
        db.ChangeTracker.Clear();

        var reopened = await db.SystemErrors.SingleAsync();
        Assert.Equal(SystemErrorStatus.New, reopened.Status);
        Assert.Null(reopened.ResolvedAt);
        Assert.Equal("أُصلح", reopened.Notes);
    }

    [Fact]
    public void Exceptions_GroupByLocation_AndKeepTheirStack()
    {
        var first = SystemErrorOccurrenceFactory.ForException(Thrown(), 500, Context());
        var second = SystemErrorOccurrenceFactory.ForException(Thrown(), 500, Context("/api/other"));

        Assert.Equal(first.Fingerprint, second.Fingerprint);
        Assert.Equal(typeof(InvalidOperationException).FullName, first.Title);
        Assert.Contains(nameof(Thrown), first.Details);
    }

    [Fact]
    public void ClientNetworkFailures_GroupByEndpoint()
    {
        SystemErrorOccurrence Network(string path, int status) => SystemErrorOccurrenceFactory.ForClient(
            SystemErrorKind.ClientNetwork, "Request failed", null, status, Context(path));

        Assert.Equal(Network("/api/documents/1", 503).Fingerprint, Network("/api/documents/2", 503).Fingerprint);
        Assert.NotEqual(Network("/api/documents/1", 503).Fingerprint, Network("/api/archive", 503).Fingerprint);
    }

    [Fact]
    public async Task Purge_RemovesOnlyErrorsNotSeenSinceTheCutoff()
    {
        await using var db = TestContextFactory.Create();
        await SystemErrorStore.RecordAsync(db, Rejection(Now.AddDays(-100), "/api/old"), CancellationToken.None);
        await SystemErrorStore.RecordAsync(db, SystemErrorOccurrenceFactory.ForRejection("Other.Code", null, 409, Context("/api/new", Now.AddDays(-5))), CancellationToken.None);

        var deleted = await new SystemErrorRepository(db).DeleteLastSeenBeforeAsync(Now.AddDays(-30));

        Assert.Equal(1, deleted);
        Assert.Equal("Other.Code", (await db.SystemErrors.SingleAsync()).Title);
    }

    [Fact]
    public async Task List_FiltersAndSearches()
    {
        await using var db = TestContextFactory.Create();
        await SystemErrorStore.RecordAsync(db, Rejection(), CancellationToken.None);
        await SystemErrorStore.RecordAsync(db, SystemErrorOccurrenceFactory.ForException(Thrown(), 500, Context()), CancellationToken.None);
        var repository = new SystemErrorRepository(db);

        var exceptions = await repository.ListPagedAsync(new PageRequest(), new SystemErrorFilter(Kind: SystemErrorKind.ServerException));
        var searched = await repository.ListPagedAsync(new PageRequest(Search: "DocumentNotFound"), new SystemErrorFilter());

        Assert.Equal(typeof(InvalidOperationException).FullName, Assert.Single(exceptions.Items).Title);
        Assert.Equal("Correspondence.DocumentNotFound", Assert.Single(searched.Items).Title);
    }

    [Fact]
    public void ExcelExport_KeepsFormulasInertAndSurvivesControlCharacters()
    {
        var error = SystemError.Create(SystemErrorOccurrenceFactory.ForRejection("=HYPERLINK(\"x\")", "bad\u0001text", 400, Context()));

        var bytes = new SystemErrorExcelWriter().Write(new[] { error });

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);
        Assert.Equal("الحالة", sheet.Cell(1, 1).GetString());
        Assert.Equal("=HYPERLINK(\"x\")", sheet.Cell(2, 4).GetString());
        Assert.False(sheet.Cell(2, 4).HasFormula);
        Assert.Equal("badtext", sheet.Cell(2, 5).GetString());
    }

    [Fact]
    public void UpdateWork_ValidatesStatusAndNotes()
    {
        var error = SystemError.Create(Rejection());

        Assert.Equal("SystemErrors.StatusInvalid", error.UpdateWork((SystemErrorStatus)99, null, Guid.NewGuid(), Now).Error.Code);
        Assert.Equal("SystemErrors.NotesTooLong", error.UpdateWork(SystemErrorStatus.New, new string('x', 4001), Guid.NewGuid(), Now).Error.Code);
    }
}
