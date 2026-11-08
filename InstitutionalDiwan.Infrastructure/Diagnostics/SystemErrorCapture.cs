using System.Threading.Channels;
using ClosedXML.Excel;
using InstitutionalDiwan.Application.Features.Diagnostics.SystemErrors;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Diagnostics;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InstitutionalDiwan.Infrastructure.Diagnostics;

/// <summary>
/// A bounded in-memory queue, so recording an error costs a failing request nothing even when the
/// database is the thing that failed. When it is full, new occurrences are dropped.
/// </summary>
internal sealed class SystemErrorQueue : ISystemErrorQueue
{
    private readonly Channel<SystemErrorOccurrence> _channel = Channel.CreateBounded<SystemErrorOccurrence>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    public ChannelReader<SystemErrorOccurrence> Reader => _channel.Reader;

    public void Enqueue(SystemErrorOccurrence occurrence) => _channel.Writer.TryWrite(occurrence);
}

/// <summary>Merges queued occurrences into the error table, on a database context of its own.</summary>
internal sealed class SystemErrorWriterJob : BackgroundService
{
    private readonly SystemErrorQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SystemErrorWriterJob> _logger;

    public SystemErrorWriterJob(
        SystemErrorQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<SystemErrorWriterJob> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var occurrence in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DiwanDbContext>();
                await SystemErrorStore.RecordAsync(db, occurrence, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Logged only: recording a failure must never produce another recorded failure.
                _logger.LogWarning(exception, "Could not record system error {Title}.", occurrence.Title);
            }
        }
    }
}

internal static class SystemErrorStore
{
    /// <summary>Counts the occurrence on its existing row, or inserts the first one.</summary>
    public static async Task RecordAsync(
        DiwanDbContext db,
        SystemErrorOccurrence occurrence,
        CancellationToken cancellationToken)
    {
        if (await BumpAsync(db, occurrence, cancellationToken) > 0)
            return;

        db.SystemErrors.Add(SystemError.Create(occurrence));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another server instance inserted this fingerprint first.
            db.ChangeTracker.Clear();
            await BumpAsync(db, occurrence, cancellationToken);
        }
    }

    private static Task<int> BumpAsync(
        DiwanDbContext db,
        SystemErrorOccurrence occurrence,
        CancellationToken cancellationToken) =>
        db.SystemErrors
            .Where(error => error.Fingerprint == occurrence.Fingerprint)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(error => error.OccurrenceCount, error => error.OccurrenceCount + 1)
                .SetProperty(error => error.LastSeenAt, occurrence.OccurredAt)
                .SetProperty(error => error.Message, occurrence.Message)
                .SetProperty(error => error.Details, occurrence.Details)
                .SetProperty(error => error.Path, occurrence.Path)
                .SetProperty(error => error.HttpMethod, occurrence.HttpMethod)
                .SetProperty(error => error.StatusCode, occurrence.StatusCode)
                .SetProperty(error => error.LastUserId, occurrence.UserId)
                .SetProperty(error => error.LastUsername, occurrence.Username)
                .SetProperty(error => error.UserAgent, occurrence.UserAgent)
                .SetProperty(error => error.TraceId, occurrence.TraceId)
                // Seen again after being resolved means the fix did not hold.
                .SetProperty(
                    error => error.Status,
                    error => error.Status == SystemErrorStatus.Resolved ? SystemErrorStatus.New : error.Status)
                .SetProperty(
                    error => error.ResolvedAt,
                    error => error.Status == SystemErrorStatus.Resolved ? null : error.ResolvedAt),
                cancellationToken);
}

internal sealed class SystemErrorRepository : ISystemErrorRepository
{
    private readonly DiwanDbContext _context;

    public SystemErrorRepository(DiwanDbContext context) => _context = context;

    public Task<SystemError?> GetByIdAsync(Guid systemErrorId, CancellationToken cancellationToken = default) =>
        _context.SystemErrors.FirstOrDefaultAsync(error => error.SystemErrorId == systemErrorId, cancellationToken);

    public async Task<PagedList<SystemError>> ListPagedAsync(
        PageRequest page,
        SystemErrorFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = Apply(_context.SystemErrors.AsNoTracking(), page, filter);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        return new PagedList<SystemError>(items, totalCount);
    }

    public async Task<IReadOnlyList<SystemError>> ListForExportAsync(
        PageRequest page,
        SystemErrorFilter filter,
        int maxRows,
        CancellationToken cancellationToken = default) =>
        await Apply(_context.SystemErrors.AsNoTracking(), page, filter).Take(maxRows).ToListAsync(cancellationToken);

    public Task<int> DeleteLastSeenBeforeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
        _context.SystemErrors.Where(error => error.LastSeenAt < cutoffUtc).ExecuteDeleteAsync(cancellationToken);

    private static IQueryable<SystemError> Apply(
        IQueryable<SystemError> query,
        PageRequest page,
        SystemErrorFilter filter)
    {
        if (filter.Status is { } status)
            query = query.Where(error => error.Status == status);
        if (filter.Source is { } source)
            query = query.Where(error => error.Source == source);
        if (filter.Kind is { } kind)
            query = query.Where(error => error.Kind == kind);
        if (filter.LastSeenFromUtc is { } from)
            query = query.Where(error => error.LastSeenAt >= from);
        if (filter.LastSeenToUtc is { } to)
            query = query.Where(error => error.LastSeenAt <= to);

        if (page.NormalizedSearch is { } search)
        {
            var pattern = $"%{search}%";
            query = query.Where(error =>
                EF.Functions.Like(error.Title, pattern) ||
                EF.Functions.Like(error.Message, pattern) ||
                (error.Path != null && EF.Functions.Like(error.Path, pattern)));
        }

        return (page.NormalizedSortBy, page.SortDescending) switch
        {
            ("title", true) => query.OrderByDescending(error => error.Title),
            ("title", false) => query.OrderBy(error => error.Title),
            ("occurrencecount", true) => query.OrderByDescending(error => error.OccurrenceCount),
            ("occurrencecount", false) => query.OrderBy(error => error.OccurrenceCount),
            ("firstseenat", true) => query.OrderByDescending(error => error.FirstSeenAt),
            ("firstseenat", false) => query.OrderBy(error => error.FirstSeenAt),
            ("status", true) => query.OrderByDescending(error => error.Status),
            ("status", false) => query.OrderBy(error => error.Status),
            (_, false) => query.OrderBy(error => error.LastSeenAt),
            _ => query.OrderByDescending(error => error.LastSeenAt)
        };
    }
}

internal sealed class SystemErrorExcelWriter : ISystemErrorExcelWriter
{
    /// <summary>Excel's limit for the text of one cell.</summary>
    private const int MaxCellLength = 32_767;

    private static readonly string[] Headers =
    {
        "الحالة", "المصدر", "النوع", "العنوان", "الرسالة", "رمز HTTP", "الطريقة", "المسار", "عدد المرات",
        "أول ظهور (UTC)", "آخر ظهور (UTC)", "آخر مستخدم", "الملاحظات", "التفاصيل التقنية", "المعرّف"
    };

    public byte[] Write(IReadOnlyList<SystemError> errors)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("الأخطاء");
        sheet.RightToLeft = true;

        for (var column = 0; column < Headers.Length; column++)
            sheet.Cell(1, column + 1).Value = Headers[column];
        sheet.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var error in errors)
        {
            // Every text goes in as a text cell, so a message starting with "=" stays inert.
            SetText(sheet.Cell(row, 1), StatusLabel(error.Status));
            SetText(sheet.Cell(row, 2), error.Source == SystemErrorSource.Server ? "الخادم" : "الواجهة");
            SetText(sheet.Cell(row, 3), KindLabel(error.Kind));
            SetText(sheet.Cell(row, 4), error.Title);
            SetText(sheet.Cell(row, 5), error.Message);
            if (error.StatusCode is { } statusCode)
                sheet.Cell(row, 6).Value = statusCode;
            SetText(sheet.Cell(row, 7), error.HttpMethod);
            SetText(sheet.Cell(row, 8), error.Path);
            sheet.Cell(row, 9).Value = error.OccurrenceCount;
            sheet.Cell(row, 10).Value = error.FirstSeenAt;
            sheet.Cell(row, 11).Value = error.LastSeenAt;
            SetText(sheet.Cell(row, 12), error.LastUsername);
            SetText(sheet.Cell(row, 13), error.Notes);
            SetText(sheet.Cell(row, 14), error.Details);
            SetText(sheet.Cell(row, 15), error.SystemErrorId.ToString());
            row++;
        }

        sheet.Columns(10, 11).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
        sheet.SheetView.FreezeRows(1);
        sheet.RangeUsed()?.SetAutoFilter();
        foreach (var column in new[] { 1, 2, 3, 6, 7, 9, 10, 11, 12 })
            sheet.Column(column).AdjustToContents(1, Math.Min(row, 200));
        foreach (var column in new[] { 4, 5, 8, 13, 14 })
            sheet.Column(column).Width = 50;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void SetText(IXLCell cell, string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // XML, and so xlsx, cannot carry control characters other than tab and line breaks.
        var clean = new string(text.Where(ch => !char.IsControl(ch) || ch is '\t' or '\n' or '\r').ToArray());
        cell.SetValue(clean.Length > MaxCellLength ? clean[..MaxCellLength] : clean);
        cell.Style.Alignment.WrapText = false;
    }

    private static string StatusLabel(SystemErrorStatus status) => status switch
    {
        SystemErrorStatus.InProgress => "قيد المعالجة",
        SystemErrorStatus.Resolved => "محلول",
        _ => "جديد"
    };

    private static string KindLabel(SystemErrorKind kind) => kind switch
    {
        SystemErrorKind.ServerRejection => "طلب مرفوض",
        SystemErrorKind.ClientRender => "انهيار صفحة",
        SystemErrorKind.ClientScript => "خطأ برمجي في المتصفح",
        SystemErrorKind.ClientNetwork => "فشل الاتصال",
        _ => "استثناء في الخادم"
    };
}
