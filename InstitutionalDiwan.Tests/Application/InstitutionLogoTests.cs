using System.Text;
using InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

/// <summary>The institution's name and uploaded logo: what is accepted, what is stored, and what the public identity shows.</summary>
public sealed class InstitutionLogoTests
{
    private static readonly byte[] Png =
        { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };

    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

    private static byte[] Webp() => Encoding.ASCII.GetBytes("RIFF....WEBPVP8 ");

    private static byte[] Svg(string body) =>
        Encoding.UTF8.GetBytes($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 10 10\">{body}</svg>");

    private static (FakeInstitutionSettingRepository Settings, FakeUnitOfWork Uow, InstitutionSetting Setting) World()
    {
        var settings = new FakeInstitutionSettingRepository();
        var setting = InstitutionSetting.Create("الديوان المؤسسي", 1, 1, "Institutional Diwan").Value;
        settings.Add(setting);
        return (settings, new FakeUnitOfWork(), setting);
    }

    [Theory]
    [InlineData("png")]
    [InlineData("jpeg")]
    [InlineData("webp")]
    [InlineData("svg")]
    public void ImageKindComesFromTheBytes(string kind)
    {
        var (data, expected) = kind switch
        {
            "png" => (Png, "image/png"),
            "jpeg" => (Jpeg, "image/jpeg"),
            "webp" => (Webp(), "image/webp"),
            _ => (Svg("<circle cx=\"5\" cy=\"5\" r=\"4\"/>"), "image/svg+xml")
        };

        var inspected = InstitutionLogo.Inspect(data);

        Assert.True(inspected.IsSuccess, inspected.Error?.Code);
        Assert.Equal(expected, inspected.Value);
    }

    [Fact]
    public void EmptyOversizedAndUnknownFilesAreRefused()
    {
        Assert.Equal(DomainErrors.InstitutionSetting.LogoEmpty.Code, InstitutionLogo.Inspect(Array.Empty<byte>()).Error.Code);
        Assert.Equal(DomainErrors.InstitutionSetting.LogoTooLarge.Code,
            InstitutionLogo.Inspect(new byte[InstitutionLogo.MaxBytes + 1]).Error.Code);
        Assert.Equal(DomainErrors.InstitutionSetting.LogoTypeNotAllowed.Code,
            InstitutionLogo.Inspect(Encoding.UTF8.GetBytes("%PDF-1.7 not an image")).Error.Code);
        Assert.Equal(DomainErrors.InstitutionSetting.LogoTypeNotAllowed.Code,
            InstitutionLogo.Inspect(Encoding.UTF8.GetBytes("MZ plain text pretending")).Error.Code);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<circle onload=\"alert(1)\" r=\"1\"/>")]
    [InlineData("<a href=\"javascript:alert(1)\"><circle r=\"1\"/></a>")]
    [InlineData("<foreignObject><div/></foreignObject>")]
    public void AnSvgThatCanRunCodeIsRefused(string body)
    {
        var inspected = InstitutionLogo.Inspect(Svg(body));

        Assert.True(inspected.IsFailure);
        Assert.Equal(DomainErrors.InstitutionSetting.LogoTypeNotAllowed.Code, inspected.Error.Code);
    }

    [Fact]
    public async Task SettingTheLogoStoresItAndStampsTheSettingVersion()
    {
        var (settings, uow, setting) = World();
        Assert.Null(setting.LogoUpdatedAt);

        var result = await new SetInstitutionLogoCommandHandler(settings, uow)
            .Handle(new SetInstitutionLogoCommand(Png), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.NotNull(result.Value.LogoUpdatedAt);
        Assert.Equal(setting.LogoUpdatedAt, result.Value.LogoUpdatedAt);
        Assert.Equal("image/png", settings.Logo!.ContentType);
        Assert.Equal(Png, settings.Logo.Data);
        Assert.Equal(1, uow.SaveChangesCallCount);
    }

    [Fact]
    public async Task ASecondUploadReplacesTheFirst()
    {
        var (settings, uow, _) = World();
        var handler = new SetInstitutionLogoCommandHandler(settings, uow);
        await handler.Handle(new SetInstitutionLogoCommand(Png), CancellationToken.None);
        var first = settings.Logo;

        var result = await handler.Handle(new SetInstitutionLogoCommand(Jpeg), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(first, settings.Logo);
        Assert.Equal("image/jpeg", settings.Logo!.ContentType);
        Assert.Equal(Jpeg, settings.Logo.Data);
    }

    [Fact]
    public async Task ARefusedFileLeavesTheCurrentLogoAlone()
    {
        var (settings, uow, setting) = World();
        var handler = new SetInstitutionLogoCommandHandler(settings, uow);
        await handler.Handle(new SetInstitutionLogoCommand(Png), CancellationToken.None);
        var version = setting.LogoUpdatedAt;

        var result = await handler.Handle(
            new SetInstitutionLogoCommand(Encoding.UTF8.GetBytes("not an image")), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(Png, settings.Logo!.Data);
        Assert.Equal(version, setting.LogoUpdatedAt);
    }

    [Fact]
    public async Task SettingTheLogoNeedsSettings()
    {
        var result = await new SetInstitutionLogoCommandHandler(new FakeInstitutionSettingRepository(), new FakeUnitOfWork())
            .Handle(new SetInstitutionLogoCommand(Png), CancellationToken.None);

        Assert.Equal(DomainErrors.InstitutionSetting.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task RemovingTheLogoClearsItAndIsHarmlessWhenThereIsNone()
    {
        var (settings, uow, setting) = World();
        await new SetInstitutionLogoCommandHandler(settings, uow)
            .Handle(new SetInstitutionLogoCommand(Png), CancellationToken.None);
        var remove = new RemoveInstitutionLogoCommandHandler(settings, uow);

        var removed = await remove.Handle(new RemoveInstitutionLogoCommand(), CancellationToken.None);
        var again = await remove.Handle(new RemoveInstitutionLogoCommand(), CancellationToken.None);

        Assert.True(removed.IsSuccess);
        Assert.Null(settings.Logo);
        Assert.Null(setting.LogoUpdatedAt);
        Assert.Null(removed.Value.LogoUpdatedAt);
        Assert.True(again.IsSuccess);
    }

    [Fact]
    public async Task BrandingShowsTheNameAndTheLogoVersion()
    {
        var (settings, uow, _) = World();
        var query = new GetBrandingQueryHandler(settings);

        var before = await query.Handle(new GetBrandingQuery(), CancellationToken.None);
        await new SetInstitutionLogoCommandHandler(settings, uow)
            .Handle(new SetInstitutionLogoCommand(Png), CancellationToken.None);
        var after = await query.Handle(new GetBrandingQuery(), CancellationToken.None);

        Assert.Equal("الديوان المؤسسي", before.Value.InstitutionNameAr);
        Assert.Equal("Institutional Diwan", before.Value.InstitutionNameEn);
        Assert.Null(before.Value.LogoUpdatedAt);
        Assert.NotNull(after.Value.LogoUpdatedAt);
    }

    [Fact]
    public async Task ReadingTheLogoFailsWithoutOneAndReturnsItsBytesWithOne()
    {
        var (settings, uow, _) = World();
        var query = new GetInstitutionLogoQueryHandler(settings);

        var none = await query.Handle(new GetInstitutionLogoQuery(), CancellationToken.None);
        await new SetInstitutionLogoCommandHandler(settings, uow)
            .Handle(new SetInstitutionLogoCommand(Jpeg), CancellationToken.None);
        var some = await query.Handle(new GetInstitutionLogoQuery(), CancellationToken.None);

        Assert.Equal(DomainErrors.InstitutionSetting.LogoNotFound.Code, none.Error.Code);
        Assert.Equal("image/jpeg", some.Value.ContentType);
        Assert.Equal(Jpeg, some.Value.Data);
    }
}
