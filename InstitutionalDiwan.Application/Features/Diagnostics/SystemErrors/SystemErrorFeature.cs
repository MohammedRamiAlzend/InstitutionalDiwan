using FluentValidation;
using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Time;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Diagnostics;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Diagnostics.SystemErrors;

/// <summary>Accepts occurrences without ever slowing or failing the request that observed them.</summary>
public interface ISystemErrorQueue
{
    void Enqueue(SystemErrorOccurrence occurrence);
}

/// <summary>Builds the Excel workbook of an error export.</summary>
public interface ISystemErrorExcelWriter
{
    byte[] Write(IReadOnlyList<SystemError> errors);
}

/// <summary>A row of the error list; the stack trace comes with the details.</summary>
public sealed record SystemErrorSummaryResponse(
    Guid SystemErrorId,
    SystemErrorSource Source,
    SystemErrorKind Kind,
    string Title,
    string Message,
    string? Path,
    string? HttpMethod,
    int? StatusCode,
    string? LastUsername,
    int OccurrenceCount,
    DateTime FirstSeenAt,
    DateTime LastSeenAt,
    SystemErrorStatus Status)
{
    public static SystemErrorSummaryResponse From(SystemError error) => new(
        error.SystemErrorId, error.Source, error.Kind, error.Title, error.Message, error.Path, error.HttpMethod,
        error.StatusCode, error.LastUsername, error.OccurrenceCount, error.FirstSeenAt, error.LastSeenAt, error.Status);
}

public sealed record SystemErrorDetailsResponse(
    SystemErrorSummaryResponse Summary,
    string? Details,
    string? UserAgent,
    string? TraceId,
    string? Notes,
    DateTime? ResolvedAt)
{
    public static SystemErrorDetailsResponse From(SystemError error) => new(
        SystemErrorSummaryResponse.From(error), error.Details, error.UserAgent, error.TraceId, error.Notes, error.ResolvedAt);
}

public sealed record SystemErrorExportFile(byte[] Content, string FileName);

public sealed record ListSystemErrorsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = true,
    SystemErrorStatus? Status = null,
    SystemErrorSource? Source = null,
    SystemErrorKind? Kind = null,
    DateTime? LastSeenFromUtc = null,
    DateTime? LastSeenToUtc = null) : IQuery<PagedResult<SystemErrorSummaryResponse>>;

public sealed record GetSystemErrorQuery(Guid SystemErrorId) : IQuery<SystemErrorDetailsResponse>;

/// <summary>Everything the current filters match, up to <see cref="MaxRows"/> rows.</summary>
public sealed record ExportSystemErrorsQuery(
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = true,
    SystemErrorStatus? Status = null,
    SystemErrorSource? Source = null,
    SystemErrorKind? Kind = null,
    DateTime? LastSeenFromUtc = null,
    DateTime? LastSeenToUtc = null) : IQuery<SystemErrorExportFile>
{
    public const int MaxRows = 10_000;
}

public sealed record UpdateSystemErrorCommand(Guid SystemErrorId, SystemErrorStatus Status, string? Notes)
    : ICommand<SystemErrorDetailsResponse>;

/// <summary>Permanently removes the errors not seen in the last <paramref name="OlderThanDays"/> days.</summary>
public sealed record PurgeSystemErrorsCommand(int OlderThanDays) : ICommand<int>;

public sealed record ReportClientErrorCommand(
    SystemErrorKind Kind,
    string Message,
    string? Details,
    string? Path,
    string? HttpMethod,
    int? StatusCode,
    string? UserAgent) : ICommand;

public sealed class PurgeSystemErrorsCommandValidator : AbstractValidator<PurgeSystemErrorsCommand>
{
    public PurgeSystemErrorsCommandValidator() =>
        RuleFor(command => command.OlderThanDays).InclusiveBetween(1, 3650).WithErrorCode("SystemErrors.PurgeDaysInvalid");
}

public sealed class ReportClientErrorCommandValidator : AbstractValidator<ReportClientErrorCommand>
{
    public ReportClientErrorCommandValidator()
    {
        RuleFor(command => command.Kind)
            .Must(kind => kind is SystemErrorKind.ClientRender or SystemErrorKind.ClientScript or SystemErrorKind.ClientNetwork)
            .WithErrorCode("SystemErrors.ReportInvalid");
        RuleFor(command => command.Message).NotEmpty().WithErrorCode("SystemErrors.ReportInvalid");
    }
}

internal sealed class SystemErrorQueryHandlers :
    IQueryHandler<ListSystemErrorsQuery, PagedResult<SystemErrorSummaryResponse>>,
    IQueryHandler<GetSystemErrorQuery, SystemErrorDetailsResponse>,
    IQueryHandler<ExportSystemErrorsQuery, SystemErrorExportFile>
{
    private readonly ISystemErrorRepository _repository;
    private readonly ISystemErrorExcelWriter _excelWriter;
    private readonly IDateTimeProvider _clock;

    public SystemErrorQueryHandlers(
        ISystemErrorRepository repository,
        ISystemErrorExcelWriter excelWriter,
        IDateTimeProvider clock)
    {
        _repository = repository;
        _excelWriter = excelWriter;
        _clock = clock;
    }

    public async Task<Result<PagedResult<SystemErrorSummaryResponse>>> Handle(
        ListSystemErrorsQuery request,
        CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize, request.Search, request.SortBy, request.SortDescending);
        var errors = await _repository.ListPagedAsync(
            page,
            new SystemErrorFilter(request.Status, request.Source, request.Kind, request.LastSeenFromUtc, request.LastSeenToUtc),
            cancellationToken);

        return Result.Success(new PagedResult<SystemErrorSummaryResponse>(
            errors.Items.Select(SystemErrorSummaryResponse.From).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            errors.TotalCount));
    }

    public async Task<Result<SystemErrorDetailsResponse>> Handle(
        GetSystemErrorQuery request,
        CancellationToken cancellationToken)
    {
        var error = await _repository.GetByIdAsync(request.SystemErrorId, cancellationToken);
        return error is null
            ? Result.Failure<SystemErrorDetailsResponse>(DomainErrors.SystemErrors.NotFound)
            : SystemErrorDetailsResponse.From(error);
    }

    public async Task<Result<SystemErrorExportFile>> Handle(
        ExportSystemErrorsQuery request,
        CancellationToken cancellationToken)
    {
        var errors = await _repository.ListForExportAsync(
            new PageRequest(1, PageRequest.MaxPageSize, request.Search, request.SortBy, request.SortDescending),
            new SystemErrorFilter(request.Status, request.Source, request.Kind, request.LastSeenFromUtc, request.LastSeenToUtc),
            ExportSystemErrorsQuery.MaxRows,
            cancellationToken);

        return new SystemErrorExportFile(_excelWriter.Write(errors), $"system-errors-{_clock.UtcNow:yyyyMMdd-HHmm}.xlsx");
    }
}

internal sealed class SystemErrorCommandHandlers :
    ICommandHandler<UpdateSystemErrorCommand, SystemErrorDetailsResponse>,
    ICommandHandler<PurgeSystemErrorsCommand, int>,
    ICommandHandler<ReportClientErrorCommand>
{
    private readonly ISystemErrorRepository _repository;
    private readonly ISystemErrorQueue _queue;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public SystemErrorCommandHandlers(
        ISystemErrorRepository repository,
        ISystemErrorQueue queue,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _queue = queue;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SystemErrorDetailsResponse>> Handle(
        UpdateSystemErrorCommand request,
        CancellationToken cancellationToken)
    {
        var error = await _repository.GetByIdAsync(request.SystemErrorId, cancellationToken);
        if (error is null)
            return Result.Failure<SystemErrorDetailsResponse>(DomainErrors.SystemErrors.NotFound);

        var updated = error.UpdateWork(request.Status, request.Notes, _userContext.UserId, _unitOfWork.UtcNow);
        if (updated.IsFailure)
            return Result.Failure<SystemErrorDetailsResponse>(updated.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return SystemErrorDetailsResponse.From(error);
    }

    public async Task<Result<int>> Handle(PurgeSystemErrorsCommand request, CancellationToken cancellationToken) =>
        Result.Success(await _repository.DeleteLastSeenBeforeAsync(
            _unitOfWork.UtcNow.AddDays(-request.OlderThanDays), cancellationToken));

    public Task<Result> Handle(ReportClientErrorCommand request, CancellationToken cancellationToken)
    {
        _queue.Enqueue(SystemErrorOccurrenceFactory.ForClient(
            request.Kind,
            request.Message,
            request.Details,
            request.StatusCode,
            new SystemErrorContext(
                request.Path,
                request.HttpMethod,
                _userContext.UserId,
                _userContext.Username,
                request.UserAgent,
                null,
                _unitOfWork.UtcNow)));

        return Task.FromResult(Result.Success());
    }
}
