using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Tests.Application;

public sealed class CreateDocumentCommandValidatorTests
{
    private static readonly CreateDocumentCommandValidator Validator = new();

    private static CreateDocumentCommand Valid() =>
        new(Guid.NewGuid(), "Some title", DocumentType.OutgoingLetterId, DocumentDirection.InternalId,
            "2026/000001", 2026);

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
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(99)]
    public void InvalidDirection_Fails(int directionId)
    {
        var result = Validator.Validate(Valid() with { DirectionId = directionId });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Correspondence.DirectionInvalid");
    }

    [Theory]
    [InlineData(DocumentDirection.InternalId)]
    [InlineData(DocumentDirection.IncomingId)]
    [InlineData(DocumentDirection.OutgoingId)]
    public void AllowedDirection_Passes(int directionId)
    {
        var result = Validator.Validate(Valid() with { DirectionId = directionId });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void MissingTypeId_Fails(int documentTypeId)
    {
        var result = Validator.Validate(Valid() with { DocumentTypeId = documentTypeId });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Correspondence.TypeCodeEmpty");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingOfficialNumber_Fails(string number)
    {
        var result = Validator.Validate(Valid() with { OfficialNumberText = number });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Numbering.OfficialNumberEmpty");
    }

    [Fact]
    public void OfficialNumberTooLong_Fails()
    {
        var result = Validator.Validate(Valid() with { OfficialNumberText = new string('9', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Numbering.OfficialNumberEmpty");
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2201)]
    public void NumberingYearOutOfRange_Fails(int year)
    {
        var result = Validator.Validate(Valid() with { NumberingYear = year });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Numbering.YearInvalid");
    }

    [Theory]
    [InlineData(1900)]
    [InlineData(2026)]
    [InlineData(2200)]
    public void NumberingYearInRange_Passes(int year)
    {
        var result = Validator.Validate(Valid() with { NumberingYear = year });

        Assert.True(result.IsValid);
    }
}
