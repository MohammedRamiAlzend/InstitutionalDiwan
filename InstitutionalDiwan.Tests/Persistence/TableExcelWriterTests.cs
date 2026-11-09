using ClosedXML.Excel;
using InstitutionalDiwan.Application.Features.Exports;
using InstitutionalDiwan.Infrastructure.Exports;

namespace InstitutionalDiwan.Tests.Persistence;

public sealed class TableExcelWriterTests
{
    private static readonly string[] Headers = { "الوثيقة", "الحالة" };

    private static IXLWorksheet Open(byte[] content, out XLWorkbook workbook)
    {
        workbook = new XLWorkbook(new MemoryStream(content));
        return workbook.Worksheet(1);
    }

    [Fact]
    public void WritesARightToLeftSheetWithTheRowsAsSent()
    {
        var content = new TableExcelWriter().Write("سجل الوثائق", Headers, new[] { new[] { "سجل أساس · 77/2026", "مسجل" } });

        var sheet = Open(content, out var workbook);
        using (workbook)
        {
            Assert.Equal("سجل الوثائق", sheet.Name);
            Assert.True(sheet.RightToLeft);
            Assert.Equal("الوثيقة", sheet.Cell(1, 1).GetString());
            Assert.Equal("سجل أساس · 77/2026", sheet.Cell(2, 1).GetString());
            Assert.Equal("مسجل", sheet.Cell(2, 2).GetString());
        }
    }

    [Fact]
    public void AFormulaLikeValueStaysPlainText()
    {
        var content = new TableExcelWriter().Write("جدول", Headers, new[] { new[] { "=1+1", "ok" } });

        var sheet = Open(content, out var workbook);
        using (workbook)
        {
            Assert.False(sheet.Cell(2, 1).HasFormula);
            Assert.Equal("=1+1", sheet.Cell(2, 1).GetString());
        }
    }

    [Theory]
    [InlineData("جدول: الوثائق/2026")]
    [InlineData("عنوان طويل جدًا يتجاوز الحد المسموح به لاسم ورقة العمل في إكسل")]
    public void ATitleExcelCannotUseAsASheetNameStillWrites(string title)
    {
        var content = new TableExcelWriter().Write(title, Headers, Array.Empty<string[]>());

        var sheet = Open(content, out var workbook);
        using (workbook)
            Assert.True(sheet.Name.Length <= 31);
    }

    [Fact]
    public void Validator_RejectsRowsThatDoNotMatchTheHeaders()
    {
        var result = new ExportTableQueryValidator().Validate(
            new ExportTableQuery("جدول", Headers, new[] { new[] { "واحد" } }));

        Assert.Contains(result.Errors, error => error.ErrorCode == "Exports.Invalid");
    }

    [Fact]
    public void Validator_RejectsTooManyRows()
    {
        var rows = Enumerable.Repeat<IReadOnlyList<string>>(new[] { "a", "b" }, ExportTableQuery.MaxRows + 1).ToList();

        var result = new ExportTableQueryValidator().Validate(new ExportTableQuery("جدول", Headers, rows));

        Assert.Contains(result.Errors, error => error.ErrorCode == "Exports.TooManyRows");
    }
}
