using ClosedXML.Excel;
using InstitutionalDiwan.Application.Features.Exports;

namespace InstitutionalDiwan.Infrastructure.Exports;

internal sealed class TableExcelWriter : ITableExcelWriter
{
    /// <summary>Excel's limit for the text of one cell.</summary>
    private const int MaxCellLength = 32_767;

    /// <summary>Excel's limit for a sheet name, which also may not hold these characters.</summary>
    private const int MaxSheetNameLength = 31;
    private static readonly char[] InvalidSheetNameCharacters = { ':', '\\', '/', '?', '*', '[', ']', '\'' };

    private const int MaxColumnWidth = 60;
    private const int HeaderRow = 1;

    public byte[] Write(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName(title));
        sheet.RightToLeft = true;

        for (var column = 0; column < headers.Count; column++)
            SetText(sheet.Cell(HeaderRow, column + 1), headers[column]);

        var header = sheet.Row(HeaderRow);
        header.Style.Font.Bold = true;

        for (var row = 0; row < rows.Count; row++)
        {
            for (var column = 0; column < headers.Count; column++)
                SetText(sheet.Cell(row + HeaderRow + 1, column + 1), rows[row][column]);
        }

        sheet.SheetView.FreezeRows(HeaderRow);
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.Columns(1, headers.Count).AdjustToContents();
        foreach (var column in sheet.ColumnsUsed())
            column.Width = Math.Min(column.Width, MaxColumnWidth);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string SheetName(string title)
    {
        var name = new string(title.Where(ch => !InvalidSheetNameCharacters.Contains(ch) && !char.IsControl(ch)).ToArray()).Trim();
        if (name.Length == 0)
            return "Sheet1";

        return name.Length > MaxSheetNameLength ? name[..MaxSheetNameLength] : name;
    }

    // Every value goes in as text, so one starting with "=" stays inert.
    private static void SetText(IXLCell cell, string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var clean = new string(text.Where(ch => !char.IsControl(ch) || ch is '\t' or '\n' or '\r').ToArray());
        cell.SetValue(clean.Length > MaxCellLength ? clean[..MaxCellLength] : clean);
    }
}
