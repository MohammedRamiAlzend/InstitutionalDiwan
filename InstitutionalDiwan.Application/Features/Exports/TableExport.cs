using FluentValidation;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Application.Features.Exports;

/// <summary>Builds the Excel workbook of a table the caller already has on screen.</summary>
public interface ITableExcelWriter
{
    byte[] Write(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows);
}

/// <summary>
/// Turns the rows of a table, as the caller sees them, into a workbook. It reads nothing from the
/// database, so it can only return what the caller sent.
/// </summary>
public sealed record ExportTableQuery(
    string Title,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows) : IQuery<TableExportFile>
{
    public const int MaxColumns = 100;
    public const int MaxRows = 5000;
    public const int MaxTitleLength = 200;
}

public sealed record TableExportFile(byte[] Content, string FileName);

public sealed class ExportTableQueryValidator : AbstractValidator<ExportTableQuery>
{
    public ExportTableQueryValidator()
    {
        RuleFor(query => query.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithErrorCode("Exports.Invalid")
            .MaximumLength(ExportTableQuery.MaxTitleLength).WithErrorCode("Exports.Invalid");

        RuleFor(query => query.Headers)
            .Must(headers => headers is { Count: > 0 and <= ExportTableQuery.MaxColumns })
            .WithErrorCode("Exports.Invalid");

        RuleFor(query => query.Rows)
            .Must(rows => rows is { Count: <= ExportTableQuery.MaxRows })
            .WithErrorCode("Exports.TooManyRows");

        RuleFor(query => query)
            .Must(query => query.Rows is null || query.Headers is null || query.Rows.All(row => row.Count == query.Headers.Count))
            .WithErrorCode("Exports.Invalid");
    }
}

internal sealed class ExportTableQueryHandler : IQueryHandler<ExportTableQuery, TableExportFile>
{
    private readonly ITableExcelWriter _excelWriter;

    public ExportTableQueryHandler(ITableExcelWriter excelWriter) => _excelWriter = excelWriter;

    public Task<Result<TableExportFile>> Handle(ExportTableQuery request, CancellationToken cancellationToken)
    {
        var content = _excelWriter.Write(request.Title.Trim(), request.Headers, request.Rows);
        return Task.FromResult(Result.Success(new TableExportFile(content, "table.xlsx")));
    }
}
