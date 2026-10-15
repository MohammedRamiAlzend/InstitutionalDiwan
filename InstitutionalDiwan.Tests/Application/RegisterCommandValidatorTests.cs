using InstitutionalDiwan.Application.Features.Identity.Register;

namespace InstitutionalDiwan.Tests.Application;

public sealed class RegisterCommandValidatorTests
{
    private static readonly RegisterCommandValidator Validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = Validator.Validate(new RegisterCommand("newuser", "Str0ng!Pass"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingUsername_Fails(string username)
    {
        var result = Validator.Validate(new RegisterCommand(username, "Str0ng!Pass"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Identity.UsernameEmpty");
    }

    [Fact]
    public void TooShortUsername_Fails()
    {
        var result = Validator.Validate(new RegisterCommand("ab", "Str0ng!Pass"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Identity.UsernameTooShort");
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")] // 7 characters — below the 8 character minimum
    public void WeakPassword_Fails(string password)
    {
        var result = Validator.Validate(new RegisterCommand("newuser", password));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void EightCharacterPassword_Passes()
    {
        var result = Validator.Validate(new RegisterCommand("newuser", "12345678"));

        Assert.True(result.IsValid);
    }
}
