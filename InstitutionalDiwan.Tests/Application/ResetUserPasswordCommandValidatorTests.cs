using InstitutionalDiwan.Application.Features.Identity.Users;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ResetUserPasswordCommandValidatorTests
{
    private static readonly ResetUserPasswordCommandValidator Validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = Validator.Validate(new ResetUserPasswordCommand(
            Guid.NewGuid(), "TempPass123!"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyUserId_Fails()
    {
        var result = Validator.Validate(new ResetUserPasswordCommand(
            Guid.Empty, "TempPass123!"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Identity.UserIdEmpty");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingNewPassword_Fails(string newPassword)
    {
        var result = Validator.Validate(new ResetUserPasswordCommand(
            Guid.NewGuid(), newPassword));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Identity.PasswordEmpty");
    }

    [Fact]
    public void TooShortNewPassword_Fails()
    {
        var result = Validator.Validate(new ResetUserPasswordCommand(
            Guid.NewGuid(), "1234567"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Identity.PasswordTooShort");
    }
}
