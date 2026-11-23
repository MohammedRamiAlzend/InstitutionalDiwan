using InstitutionalDiwan.Application.Features.Drive;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class DriveReadinessTests
{
    [Fact]
    public async Task Readiness_AllConfigured_IsReadyAndNeverReturnsTheSecret()
    {
        var world = await DriveHandlerWorld.CreateAsync();

        var result = await world.Readiness().Handle(new GetDriveReadinessQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsReady);
        Assert.True(result.Value.OnlyOffice.Enabled);
        Assert.True(result.Value.OnlyOffice.ServerUrlConfigured);
        Assert.True(result.Value.OnlyOffice.CallbackBaseUrlConfigured);
        Assert.True(result.Value.OnlyOffice.JwtSecretConfigured);
        Assert.True(result.Value.OnlyOffice.IsConfigured);
        Assert.Equal("https://onlyoffice.local", result.Value.OnlyOffice.ServerUrl);
        Assert.Equal("https://api.local", result.Value.OnlyOffice.CallbackBaseUrl);
        Assert.Equal("https://api.local/api/drive/onlyoffice/callback", result.Value.OnlyOffice.CallbackUrl);
        Assert.True(result.Value.Storage.IsConfigured);
    }

    [Fact]
    public async Task Readiness_MissingSecret_MarksOnlyOfficeNotConfigured()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        world.OnlyOfficeOptions.JwtSecret = string.Empty;

        var result = await world.Readiness().Handle(new GetDriveReadinessQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.OnlyOffice.JwtSecretConfigured);
        Assert.False(result.Value.OnlyOffice.IsConfigured);
        Assert.False(result.Value.IsReady);
        Assert.True(result.Value.OnlyOffice.ServerUrlConfigured);
        Assert.True(result.Value.Storage.IsConfigured);
    }

    [Fact]
    public async Task Readiness_DisabledEditingOrMissingStorage_IsNotReady()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        world.OnlyOfficeOptions.Enabled = false;
        world.Storage.IsConfigured = false;

        var result = await world.Readiness().Handle(new GetDriveReadinessQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsReady);
        Assert.False(result.Value.OnlyOffice.Enabled);
        Assert.False(result.Value.OnlyOffice.IsConfigured);
        Assert.False(result.Value.Storage.IsConfigured);
        Assert.Equal("https://api.local/api/drive/onlyoffice/callback", result.Value.OnlyOffice.CallbackUrl);
    }

    [Fact]
    public async Task Readiness_BlankUrls_AreReportedAsNullAndCallbackIsOmitted()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        world.OnlyOfficeOptions.ServerUrl = "  ";
        world.OnlyOfficeOptions.CallbackBaseUrl = string.Empty;

        var result = await world.Readiness().Handle(new GetDriveReadinessQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.OnlyOffice.ServerUrlConfigured);
        Assert.False(result.Value.OnlyOffice.CallbackBaseUrlConfigured);
        Assert.Null(result.Value.OnlyOffice.ServerUrl);
        Assert.Null(result.Value.OnlyOffice.CallbackBaseUrl);
        Assert.Null(result.Value.OnlyOffice.CallbackUrl);
        Assert.False(result.Value.IsReady);
    }
}
