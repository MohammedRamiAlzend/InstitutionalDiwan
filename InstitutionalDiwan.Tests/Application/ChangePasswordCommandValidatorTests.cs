using InstitutionalDiwan.Application.Features.Identity.ChangePassword;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ChangePasswordCommandValidatorTests
{
    private static readonly ChangePasswordCommandValidator Validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = Validator.Validate(new ChangePasswordCommand(
            Guid.NewGuid(), "OldPass123!", "NewPass456!"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyUserId_Fails()
    {
        var result = Validator.Validate(new ChangePasswordCommand(
            Guid.Empty, "OldPass123!", "NewPass456!"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Identity.UserIdEmpty");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingOldPassword_Fails(string oldPassword)
    {
        var result = Validator.Validate(new ChangePasswordCommand(
            Guid.NewGuid(), oldPassword, "NewPass456!"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Identity.PasswordEmpty");
    }

    [Fact]
    public void TooShortNewPassword_Fails()
    {
        var result = Validator.Validate(new ChangePasswordCommand(
            Guid.NewGuid(), "OldPass123!", "1234567"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Identity.PasswordTooShort");
    }

    [Fact]
    public void NewPasswordEqualToOld_Fails()
    {
        var result = Validator.Validate(new ChangePasswordCommand(
            Guid.NewGuid(), "OldPass123!", "OldPass123!"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Identity.NewPasswordSameAsOld");
    }
}
