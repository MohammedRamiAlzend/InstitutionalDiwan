using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Infrastructure.Storage;

namespace InstitutionalDiwan.Tests.Storage;

public sealed class FileSignatureValidatorTests
{
    private readonly FileSignatureValidator _validator = new();

    private static DocumentFileType FileType(string extensions = ".pdf,.png,.jpg,.tif,.tiff,.docx,.doc,.svg") =>
        DocumentFileType.Create(1, "Test", "اختبار", extensions, 1024).Value;

    [Fact]
    public void AcceptsPdfMagicBytes()
    {
        var result = _validator.Validate("%PDF-1.7"u8, "scan.pdf", 8, FileType());

        Assert.True(result.IsSuccess, result.Error?.Code);
    }

    [Fact]
    public void RejectsARenamedExecutableWithAPdfExtension()
    {
        var result = _validator.Validate([0x4D, 0x5A, 0x90, 0x00], "invoice.pdf", 4, FileType());

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Attachments.SignatureMismatch.Code, result.Error.Code);
    }

    [Theory]
    [InlineData(".png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })]
    [InlineData(".jpg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })]
    [InlineData(".tif", new byte[] { 0x49, 0x49, 0x2A, 0x00 })]
    [InlineData(".docx", new byte[] { 0x50, 0x4B, 0x03, 0x04 })]
    [InlineData(".doc", new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 })]
    public void AcceptsKnownMagicBytes(string extension, byte[] header)
    {
        var result = _validator.Validate(header, "file" + extension, header.Length, FileType());

        Assert.True(result.IsSuccess, result.Error?.Code);
    }

    [Fact]
    public void AcceptsSvgText()
    {
        var result = _validator.Validate("<svg xmlns='http://www.w3.org/2000/svg'>"u8, "seal.svg", 40, FileType());

        Assert.True(result.IsSuccess, result.Error?.Code);
    }

    [Fact]
    public void RejectsAnExtensionOutsideTheAllowList()
    {
        var result = _validator.Validate("%PDF-"u8, "script.exe", 5, FileType());

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Attachments.ExtensionNotAllowed.Code, result.Error.Code);
    }

    [Fact]
    public void RejectsAnEmptyFile()
    {
        var result = _validator.Validate([], "scan.pdf", 0, FileType());

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Attachments.FileEmpty.Code, result.Error.Code);
    }

    [Fact]
    public void RejectsAFileLargerThanTheTypeLimit()
    {
        var result = _validator.Validate("%PDF-"u8, "scan.pdf", 2048, FileType());

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Attachments.FileTooLarge.Code, result.Error.Code);
    }
}
