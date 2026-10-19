using InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class InstitutionSettingsHandlerTests
{
    private static FakeLookupRepository CreateLookups(bool withEntries = true)
    {
        var lookups = new FakeLookupRepository();
        if (!withEntries)
            return lookups;

        lookups.Add("numbering-modes", NumberingMode.Create(NumberingMode.AutomaticId, "Automatic", "تلقائي").Value);
        lookups.Add("numbering-reset-policies", NumberingResetPolicy.Create(NumberingResetPolicy.YearlyId, "Yearly", "سنوي").Value);
        return lookups;
    }

    private static Department CreateDepartment(
        int typeId = DepartmentType.CentralDiwanId,
        Guid? parentDepartmentId = null,
        bool active = true)
    {
        var department = Department.Create(
            $"D-{Guid.NewGuid():N}"[..10], "الديوان المركزي", typeId, parentDepartmentId);

        Assert.True(department.IsSuccess);

        if (!active)
            department.Value.Deactivate(hasActiveChildren: false);

        return department.Value;
    }

    private static InstitutionSetting CreateSetting(Guid? centralDiwanDepartmentId)
    {
        var setting = InstitutionSetting.Create(
            "المؤسسة",
            NumberingMode.AutomaticId,
            NumberingResetPolicy.YearlyId,
            centralDiwanDepartmentId: centralDiwanDepartmentId);

        Assert.True(setting.IsSuccess);
        return setting.Value;
    }

    private static UpdateInstitutionSettingsCommandHandler CreateHandler(
        FakeInstitutionSettingRepository settings,
        FakeDepartmentRepository departments,
        FakeLookupRepository lookups,
        FakeEmployeeRepository? employees = null,
        FakeUnitOfWork? unitOfWork = null)
        => new(settings, departments, employees ?? new FakeEmployeeRepository(), lookups, unitOfWork ?? new FakeUnitOfWork());

    [Fact]
    public async Task Update_WithValidPayload_UpdatesSettings()
    {
        var centralDiwan = CreateDepartment();
        var setting = CreateSetting(centralDiwan.DepartmentId);

        var settings = new FakeInstitutionSettingRepository();
        settings.Add(setting);

        var departments = new FakeDepartmentRepository();
        departments.Add(centralDiwan);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateHandler(settings, departments, CreateLookups(), unitOfWork: unitOfWork)
            .Handle(new UpdateInstitutionSettingsCommand(
                "وزارة الداخلية",
                NumberingMode.AutomaticId,
                NumberingResetPolicy.YearlyId,
                centralDiwan.DepartmentId,
                InstitutionNameEn: "Ministry of Interior",
                DefaultTimezone: "Asia/Damascus"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("وزارة الداخلية", result.Value.InstitutionNameAr);
        Assert.Equal("Ministry of Interior", result.Value.InstitutionNameEn);
        Assert.Equal(centralDiwan.DepartmentId, result.Value.CentralDiwanDepartmentId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Update_WithoutSettings_ReturnsNotFound()
    {
        var result = await CreateHandler(
                new FakeInstitutionSettingRepository(),
                new FakeDepartmentRepository(),
                CreateLookups())
            .Handle(new UpdateInstitutionSettingsCommand(
                "المؤسسة", NumberingMode.AutomaticId, NumberingResetPolicy.YearlyId, Guid.NewGuid()),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.InstitutionSetting.NotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Update_WhenCentralDiwanDoesNotExist_ReturnsValidationError()
    {
        var setting = CreateSetting(Guid.NewGuid());
        var settings = new FakeInstitutionSettingRepository();
        settings.Add(setting);

        var result = await CreateHandler(settings, new FakeDepartmentRepository(), CreateLookups())
            .Handle(new UpdateInstitutionSettingsCommand(
                "المؤسسة", NumberingMode.AutomaticId, NumberingResetPolicy.YearlyId, Guid.NewGuid()),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.InstitutionSetting.CentralDiwanNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Update_WhenCentralDiwanIsNotCentralDiwanType_ReturnsConflict()
    {
        var wrongType = CreateDepartment(DepartmentType.SectionId);
        var setting = CreateSetting(wrongType.DepartmentId);

        var settings = new FakeInstitutionSettingRepository();
        settings.Add(setting);

        var departments = new FakeDepartmentRepository();
        departments.Add(wrongType);

        var result = await CreateHandler(settings, departments, CreateLookups())
            .Handle(new UpdateInstitutionSettingsCommand(
                "المؤسسة", NumberingMode.AutomaticId, NumberingResetPolicy.YearlyId, wrongType.DepartmentId),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.InstitutionSetting.CentralDiwanTypeInvalid.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Update_WhenCentralDiwanHasParent_ReturnsConflict()
    {
        var parent = CreateDepartment(DepartmentType.DirectorateId);
        var child = CreateDepartment(DepartmentType.CentralDiwanId, parent.DepartmentId);
        var setting = CreateSetting(child.DepartmentId);

        var settings = new FakeInstitutionSettingRepository();
        settings.Add(setting);

        var departments = new FakeDepartmentRepository();
        departments.Add(parent);
        departments.Add(child);

        var result = await CreateHandler(settings, departments, CreateLookups())
            .Handle(new UpdateInstitutionSettingsCommand(
                "المؤسسة", NumberingMode.AutomaticId, NumberingResetPolicy.YearlyId, child.DepartmentId),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.InstitutionSetting.CentralDiwanNotRoot.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Update_WhenMovingCentralDiwanWithoutReason_ReturnsValidationError()
    {
        var current = CreateDepartment();
        var next = CreateDepartment();
        var setting = CreateSetting(current.DepartmentId);

        var settings = new FakeInstitutionSettingRepository();
        settings.Add(setting);

        var departments = new FakeDepartmentRepository();
        departments.Add(current);
        departments.Add(next);

        var result = await CreateHandler(settings, departments, CreateLookups())
            .Handle(new UpdateInstitutionSettingsCommand(
                "المؤسسة", NumberingMode.AutomaticId, NumberingResetPolicy.YearlyId, next.DepartmentId),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            DomainErrors.InstitutionSetting.CentralDiwanTransferReasonRequired.Code,
            result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Update_WhenMovingCentralDiwanWithReason_Succeeds()
    {
        var current = CreateDepartment();
        var next = CreateDepartment();
        var setting = CreateSetting(current.DepartmentId);

        var settings = new FakeInstitutionSettingRepository();
        settings.Add(setting);

        var departments = new FakeDepartmentRepository();
        departments.Add(current);
        departments.Add(next);

        var result = await CreateHandler(settings, departments, CreateLookups())
            .Handle(new UpdateInstitutionSettingsCommand(
                "المؤسسة",
                NumberingMode.AutomaticId,
                NumberingResetPolicy.YearlyId,
                next.DepartmentId,
                CentralDiwanTransferReason: "قرار إداري رقم 12"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(next.DepartmentId, result.Value.CentralDiwanDepartmentId);
    }

    [Fact]
    public async Task Update_WhenNumberingModeIsUnknown_ReturnsValidationError()
    {
        var centralDiwan = CreateDepartment();
        var setting = CreateSetting(centralDiwan.DepartmentId);

        var settings = new FakeInstitutionSettingRepository();
        settings.Add(setting);

        var departments = new FakeDepartmentRepository();
        departments.Add(centralDiwan);

        var result = await CreateHandler(settings, departments, CreateLookups())
            .Handle(new UpdateInstitutionSettingsCommand(
                "المؤسسة", 99, NumberingResetPolicy.YearlyId, centralDiwan.DepartmentId),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.InstitutionSetting.NumberingModeNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Get_WhenSettingsAreMissing_ReturnsNotFound()
    {
        var result = await new GetInstitutionSettingsQueryHandler(new FakeInstitutionSettingRepository())
            .Handle(new GetInstitutionSettingsQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.InstitutionSetting.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Get_WhenSettingsExist_ReturnsSingleton()
    {
        var centralDiwan = CreateDepartment();
        var settings = new FakeInstitutionSettingRepository();
        settings.Add(CreateSetting(centralDiwan.DepartmentId));

        var result = await new GetInstitutionSettingsQueryHandler(settings)
            .Handle(new GetInstitutionSettingsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(centralDiwan.DepartmentId, result.Value.CentralDiwanDepartmentId);
    }
}
