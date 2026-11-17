using InstitutionalDiwan.Application.Features.Ui.UiLabels;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Ui;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class UiLabelOverrideTests
{
    private static readonly DateTime FrozenNow = new(2026, 11, 17, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidKeyAndValue_NormalizesAndStamps()
    {
        var editorId = Guid.NewGuid();

        var result = UiLabelOverride.Create("  Nav.Documents ", " الأضابير ", editorId, FrozenNow);

        Assert.True(result.IsSuccess);
        Assert.Equal("nav.documents", result.Value.Key);
        Assert.Equal("الأضابير", result.Value.Value);
        Assert.Equal(FrozenNow, result.Value.UpdatedAt);
        Assert.Equal(editorId, result.Value.UpdatedByUserId);
        Assert.NotEqual(Guid.Empty, result.Value.LabelId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".documents")]
    [InlineData("nav..documents")]
    [InlineData("Nav Documents")]
    [InlineData("وثائق")]
    public void Create_WithInvalidKey_FailsValidation(string? key)
    {
        var result = UiLabelOverride.Create(key, "الأضابير", utcNow: FrozenNow);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.UiLabel.KeyInvalid.Code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void Create_WithTooLongKey_FailsValidation()
    {
        var result = UiLabelOverride.Create(new string('a', UiLabelOverride.MaxKeyLength + 1), "قيمة");

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.UiLabel.KeyInvalid.Code, result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyValue_FailsValidation(string? value)
    {
        var result = UiLabelOverride.Create("nav.documents", value);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.UiLabel.ValueEmpty.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithTooLongValue_FailsValidation()
    {
        var result = UiLabelOverride.Create("nav.documents", new string('ا', UiLabelOverride.MaxValueLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.UiLabel.ValueTooLong.Code, result.Error.Code);
    }

    [Fact]
    public void UpdateValue_ReplacesTextAndRestamps()
    {
        var created = UiLabelOverride.Create("nav.documents", "الأضابير", utcNow: FrozenNow);
        var editorId = Guid.NewGuid();
        var later = FrozenNow.AddHours(1);

        var updated = created.Value.UpdateValue("المعاملات", editorId, later);

        Assert.True(updated.IsSuccess);
        Assert.Equal("المعاملات", created.Value.Value);
        Assert.Equal(later, created.Value.UpdatedAt);
        Assert.Equal(editorId, created.Value.UpdatedByUserId);
    }

    [Fact]
    public void UpdateValue_WithEmptyValue_FailsAndKeepsOldText()
    {
        var created = UiLabelOverride.Create("nav.documents", "الأضابير", utcNow: FrozenNow);

        var updated = created.Value.UpdateValue("  ", utcNow: FrozenNow.AddHours(1));

        Assert.True(updated.IsFailure);
        Assert.Equal(DomainErrors.UiLabel.ValueEmpty.Code, updated.Error.Code);
        Assert.Equal("الأضابير", created.Value.Value);
        Assert.Equal(FrozenNow, created.Value.UpdatedAt);
    }
}

public sealed class UiLabelOverrideHandlerTests
{
    private static readonly DateTime FrozenNow = new(2026, 11, 17, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Get_WhenEmpty_ReturnsVersionZeroAndNoOverrides()
    {
        var repository = new FakeUiLabelOverrideRepository();

        var result = await new GetUiLabelOverridesQueryHandler(repository).Handle(
            new GetUiLabelOverridesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Version);
        Assert.Empty(result.Value.Overrides);
    }

    [Fact]
    public async Task Set_WhenKeyIsNew_CreatesOverrideAndSaves()
    {
        var repository = new FakeUiLabelOverrideRepository();
        var unitOfWork = new FakeUnitOfWork { UtcNow = FrozenNow };
        var editorId = Guid.NewGuid();

        var result = await new SetUiLabelOverrideCommandHandler(repository, unitOfWork).Handle(
            new SetUiLabelOverrideCommand("nav.documents", "الأضابير", editorId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Single(repository.Overrides);
        Assert.Equal("الأضابير", result.Value.Overrides["nav.documents"]);
        Assert.Equal(FrozenNow.Ticks, result.Value.Version);
        Assert.Equal(editorId, repository.Overrides[0].UpdatedByUserId);
    }

    [Fact]
    public async Task Set_WhenKeyExists_UpdatesInPlaceWithoutNewRow()
    {
        var repository = new FakeUiLabelOverrideRepository();
        var existing = UiLabelOverride.Create("nav.documents", "الأضابير", utcNow: FrozenNow);
        repository.Add(existing.Value);
        var unitOfWork = new FakeUnitOfWork { UtcNow = FrozenNow.AddHours(1) };

        var result = await new SetUiLabelOverrideCommandHandler(repository, unitOfWork).Handle(
            new SetUiLabelOverrideCommand("NAV.DOCUMENTS", "المعاملات", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(repository.Overrides);
        Assert.Equal("المعاملات", result.Value.Overrides["nav.documents"]);
        Assert.Equal(FrozenNow.AddHours(1).Ticks, result.Value.Version);
    }

    [Fact]
    public async Task Set_WithInvalidValue_FailsWithoutSaving()
    {
        var repository = new FakeUiLabelOverrideRepository();
        var unitOfWork = new FakeUnitOfWork();

        var result = await new SetUiLabelOverrideCommandHandler(repository, unitOfWork).Handle(
            new SetUiLabelOverrideCommand("nav.documents", "   ", null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.UiLabel.ValueEmpty.Code, result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Empty(repository.Overrides);
    }

    [Fact]
    public async Task Set_WithInvalidKey_FailsWithoutSaving()
    {
        var repository = new FakeUiLabelOverrideRepository();
        var unitOfWork = new FakeUnitOfWork();

        var result = await new SetUiLabelOverrideCommandHandler(repository, unitOfWork).Handle(
            new SetUiLabelOverrideCommand("not a key", "قيمة", null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.UiLabel.KeyInvalid.Code, result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Reset_WhenKeyExists_RemovesOverrideAndSaves()
    {
        var repository = new FakeUiLabelOverrideRepository();
        var existing = UiLabelOverride.Create("nav.documents", "الأضابير", utcNow: FrozenNow);
        repository.Add(existing.Value);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new ResetUiLabelOverrideCommandHandler(repository, unitOfWork).Handle(
            new ResetUiLabelOverrideCommand("nav.documents"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Empty(repository.Overrides);
        Assert.Equal(0, result.Value.Version);
    }

    [Fact]
    public async Task Reset_WhenKeyIsUnknown_SucceedsWithoutSaving()
    {
        var repository = new FakeUiLabelOverrideRepository();
        var unitOfWork = new FakeUnitOfWork();

        var result = await new ResetUiLabelOverrideCommandHandler(repository, unitOfWork).Handle(
            new ResetUiLabelOverrideCommand("nav.documents"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Get_VersionTracksNewestWrite()
    {
        var repository = new FakeUiLabelOverrideRepository();
        var older = UiLabelOverride.Create("common.save", "حفظ", utcNow: FrozenNow);
        var newer = UiLabelOverride.Create("nav.documents", "الأضابير", utcNow: FrozenNow.AddMinutes(5));
        repository.Add(older.Value);
        repository.Add(newer.Value);

        var result = await new GetUiLabelOverridesQueryHandler(repository).Handle(
            new GetUiLabelOverridesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(FrozenNow.AddMinutes(5).Ticks, result.Value.Version);
        Assert.Equal(2, result.Value.Overrides.Count);
    }
}

public sealed class SetUiLabelOverrideCommandValidatorTests
{
    private readonly SetUiLabelOverrideCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var result = _validator.Validate(new SetUiLabelOverrideCommand("nav.documents", "الأضابير", null));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nav documents")]
    [InlineData(".nav")]
    public void Validate_WithInvalidKey_ReportsKeyInvalid(string key)
    {
        var result = _validator.Validate(new SetUiLabelOverrideCommand(key, "قيمة", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "UiLabel.KeyInvalid");
    }

    [Fact]
    public void Validate_WithEmptyValue_ReportsValueEmpty()
    {
        var result = _validator.Validate(new SetUiLabelOverrideCommand("nav.documents", " ", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "UiLabel.ValueEmpty");
    }

    [Fact]
    public void Validate_WithTooLongValue_ReportsValueTooLong()
    {
        var result = _validator.Validate(new SetUiLabelOverrideCommand(
            "nav.documents", new string('ا', UiLabelOverride.MaxValueLength + 1), null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "UiLabel.ValueTooLong");
    }
}