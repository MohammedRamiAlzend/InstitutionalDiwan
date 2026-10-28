using InstitutionalDiwan.Application.Features.Organization.Departments;
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

    [Theory]
    [InlineData("update")]
    [InlineData("move")]
    [InlineData("delete")]
    public async Task DepartmentMutation_RequiresSettingsTransfer_ThenProtectsOnlyNewCentralDiwan(string operation)
    {
        var current = CreateDepartment();
        var next = CreateDepartment();
        var parent = CreateDepartment(DepartmentType.DirectorateId);
        current.ClearDomainEvents();
        next.ClearDomainEvents();
        var setting = CreateSetting(current.DepartmentId);
        var settings = new FakeInstitutionSettingRepository();
        settings.Add(setting);
        var departments = new FakeDepartmentRepository();
        departments.Add(current);
        departments.Add(next);
        departments.Add(parent);
        var unitOfWork = new FakeUnitOfWork();
        var updateHandler = new UpdateDepartmentCommandHandler(
            departments, new FakeEmployeeRepository(), settings, unitOfWork);
        var moveHandler = new MoveDepartmentCommandHandler(departments, settings, unitOfWork);
        var deleteHandler = new DeleteDepartmentCommandHandler(departments, settings, unitOfWork);

        async Task<Result> MutateAsync(Guid departmentId) => operation switch
        {
            "update" => await updateHandler.Handle(
                new UpdateDepartmentCommand(departmentId, "اسم جديد", parent.DepartmentId, true, true, Notes: "ملاحظة"),
                CancellationToken.None),
            "move" => await moveHandler.Handle(
                new MoveDepartmentCommand(departmentId, parent.DepartmentId), CancellationToken.None),
            "delete" => await deleteHandler.Handle(
                new DeleteDepartmentCommand(departmentId), CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        var blocked = await MutateAsync(current.DepartmentId);

        Assert.True(blocked.IsFailure);
        Assert.Equal(DomainErrors.Department.CentralDiwanProtected.Code, blocked.Error.Code);
        Assert.Equal(ErrorType.Conflict, blocked.Error.Type);
        Assert.True(current.IsActive);
        Assert.Null(current.ParentDepartmentId);
        Assert.Equal("الديوان المركزي", current.NameAr);
        Assert.Null(current.Notes);
        Assert.False(current.CanSendExternal);
        Assert.False(current.CanReceiveExternal);
        Assert.Null(current.UpdatedAt);
        Assert.Empty(current.DomainEvents);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);

        var transfer = await CreateHandler(settings, departments, CreateLookups(), unitOfWork: unitOfWork)
            .Handle(new UpdateInstitutionSettingsCommand(
                "المؤسسة", NumberingMode.AutomaticId, NumberingResetPolicy.YearlyId, next.DepartmentId,
                CentralDiwanTransferReason: "قرار نقل الديوان"), CancellationToken.None);

        Assert.True(transfer.IsSuccess);
        Assert.Equal(next.DepartmentId, setting.CentralDiwanDepartmentId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);

        var allowed = await MutateAsync(current.DepartmentId);

        Assert.True(allowed.IsSuccess);
        if (operation == "delete")
            Assert.False(current.IsActive);
        else
            Assert.Equal(parent.DepartmentId, current.ParentDepartmentId);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);

        var newHubBlocked = await MutateAsync(next.DepartmentId);

        Assert.True(newHubBlocked.IsFailure);
        Assert.Equal(DomainErrors.Department.CentralDiwanProtected.Code, newHubBlocked.Error.Code);
        Assert.Equal(ErrorType.Conflict, newHubBlocked.Error.Type);
        Assert.True(next.IsActive);
        Assert.Null(next.ParentDepartmentId);
        Assert.Null(next.UpdatedAt);
        Assert.Empty(next.DomainEvents);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_WhenCentralDiwanIsNullOrInactive_RejectsWithoutSaving(bool missing)
    {
        var centralDiwan = CreateDepartment(active: false);
        var setting = CreateSetting(centralDiwan.DepartmentId);
        var settings = new FakeInstitutionSettingRepository();
        settings.Add(setting);
        var departments = new FakeDepartmentRepository();
        departments.Add(centralDiwan);
        var unitOfWork = new FakeUnitOfWork();
        var updatedAt = setting.UpdatedAt;

        var result = await CreateHandler(settings, departments, CreateLookups(), unitOfWork: unitOfWork)
            .Handle(new UpdateInstitutionSettingsCommand(
                "اسم جديد", NumberingMode.AutomaticId, NumberingResetPolicy.YearlyId,
                missing ? null : centralDiwan.DepartmentId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(missing
            ? DomainErrors.InstitutionSetting.CentralDiwanRequired
            : DomainErrors.InstitutionSetting.CentralDiwanInactive, result.Error);
        Assert.Equal(missing ? ErrorType.Validation : ErrorType.Conflict, result.Error.Type);
        Assert.Equal("المؤسسة", setting.InstitutionNameAr);
        Assert.Equal(centralDiwan.DepartmentId, setting.CentralDiwanDepartmentId);
        Assert.Equal(updatedAt, setting.UpdatedAt);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_WhenNumberingLookupIsInactive_ReturnsConflictWithoutSaving(bool numberingMode)
    {
        var centralDiwan = CreateDepartment();
        var setting = CreateSetting(centralDiwan.DepartmentId);
        var settings = new FakeInstitutionSettingRepository();
        settings.Add(setting);
        var departments = new FakeDepartmentRepository();
        departments.Add(centralDiwan);
        var lookups = CreateLookups();
        var entry = await lookups.GetByIdAsync(
            numberingMode ? "numbering-modes" : "numbering-reset-policies",
            numberingMode ? NumberingMode.AutomaticId : NumberingResetPolicy.YearlyId);
        Assert.NotNull(entry);
        entry.Deactivate();
        var unitOfWork = new FakeUnitOfWork();
        var updatedAt = setting.UpdatedAt;

        var result = await CreateHandler(settings, departments, lookups, unitOfWork: unitOfWork)
            .Handle(new UpdateInstitutionSettingsCommand(
                "اسم جديد", NumberingMode.AutomaticId, NumberingResetPolicy.YearlyId, centralDiwan.DepartmentId),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(numberingMode
            ? DomainErrors.InstitutionSetting.NumberingModeInactive
            : DomainErrors.InstitutionSetting.ResetPolicyInactive, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("المؤسسة", setting.InstitutionNameAr);
        Assert.Equal(updatedAt, setting.UpdatedAt);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Update_WhenResetPolicyIsUnknown_ReturnsValidationWithoutSaving()
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
                "اسم جديد", NumberingMode.AutomaticId, 99, centralDiwan.DepartmentId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.InstitutionSetting.ResetPolicyNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("المؤسسة", setting.InstitutionNameAr);
        Assert.Equal(NumberingResetPolicy.YearlyId, setting.NumberingResetPolicyId);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_WhenResponsibleEmployeeIsUnknown_ReturnsValidationWithoutSaving(bool emptyId)
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
                "اسم جديد", NumberingMode.AutomaticId, NumberingResetPolicy.YearlyId, centralDiwan.DepartmentId,
                ResponsibleEmployeeId: emptyId ? Guid.Empty : Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.InstitutionSetting.ResponsibleEmployeeNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("المؤسسة", setting.InstitutionNameAr);
        Assert.Null(setting.ResponsibleEmployeeId);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(501)]
    public void Validator_EnforcesLogoPathLimit(int? length)
    {
        var logoPath = length.HasValue ? new string('p', length.Value) : null;
        var result = new UpdateInstitutionSettingsCommandValidator().Validate(
            new UpdateInstitutionSettingsCommand(
                "المؤسسة", NumberingMode.AutomaticId, NumberingResetPolicy.YearlyId, Guid.NewGuid(),
                LogoPath: logoPath));

        Assert.Equal(length is null or <= 500, result.IsValid);
        if (!result.IsValid)
        {
            var error = Assert.Single(result.Errors);
            Assert.Equal(nameof(UpdateInstitutionSettingsCommand.LogoPath), error.PropertyName);
            Assert.Equal("MaximumLengthValidator", error.ErrorCode);
        }
    }

    [Fact]
    public void Validator_WhenCentralDiwanIsNull_ReturnsRequiredError()
    {
        var result = new UpdateInstitutionSettingsCommandValidator().Validate(
            new UpdateInstitutionSettingsCommand(
                "المؤسسة", NumberingMode.AutomaticId, NumberingResetPolicy.YearlyId, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(UpdateInstitutionSettingsCommand.CentralDiwanDepartmentId) &&
            error.ErrorCode == DomainErrors.InstitutionSetting.CentralDiwanRequired.Code);
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
