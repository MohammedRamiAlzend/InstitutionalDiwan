using InstitutionalDiwan.Application.Features.Correspondence.Documents;

namespace InstitutionalDiwan.Tests.Application;

public sealed class CreateDocumentCommandValidatorTests
{
    private static readonly CreateDocumentCommandValidator Validator = new();

    private static CreateDocumentCommand Valid() =>
        new(Guid.NewGuid(), "Some title", "LETTER", "INTERNAL");

    [Fact]
    public void ValidCommand_Passes()
    {
        Assert.True(Validator.Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingTitle_Fails(string title)
    {
        var result = Validator.Validate(Valid() with { Title = title });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Correspondence.TitleEmpty");
    }

    [Theory]
    [InlineData("sideways")]
    [InlineData("")]
    [InlineData("internal-ish")]
    public void InvalidDirection_Fails(string direction)
    {
        var result = Validator.Validate(Valid() with { Direction = direction });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Correspondence.DirectionInvalid");
    }

    [Theory]
    [InlineData("INTERNAL")]
    [InlineData("internal")]
    [InlineData(" Incoming ")]
    public void AllowedDirection_PassesRegardlessOfCase(string direction)
    {
        var result = Validator.Validate(Valid() with { Direction = direction });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void MissingTypeCode_Fails()
    {
        var result = Validator.Validate(Valid() with { DocumentTypeCode = " " });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Correspondence.TypeCodeEmpty");
    }
}
