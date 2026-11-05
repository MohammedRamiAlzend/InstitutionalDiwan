using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Tests.Application;

public sealed class CreateDocumentCommandValidatorTests
{
    private static readonly CreateDocumentCommandValidator Validator = new();

    private static CreateDocumentCommand Valid() =>
        new(Guid.NewGuid(), "Some title", DocumentType.OutgoingLetterId,
            "17/2026", new DateTime(2026, 3, 9), DirectionId: DocumentDirection.InternalId);

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

    [Fact]
    public void NullDirectionConfidentialityAndPriority_Passes()
    {
        var command = Valid() with
        {
            DirectionId = null,
            ConfidentialityLevelId = null,
            PriorityLevelId = null,
        };

        Assert.True(Validator.Validate(command).IsValid);
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
    public void MissingRegistryNumber_Fails(string number)
    {
        var result = Validator.Validate(Valid() with { DepartmentRegistryNumberText = number });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Numbering.DepartmentNumberRequired");
    }

    [Fact]
    public void RegistryNumberTooLong_Fails()
    {
        var result = Validator.Validate(Valid() with { DepartmentRegistryNumberText = new string('9', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Numbering.DepartmentNumberRequired");
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2201)]
    public void RegistryDateOutOfRange_Fails(int year)
    {
        var result = Validator.Validate(Valid() with { DepartmentRegistryDate = new DateTime(year, 1, 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Numbering.RegistryDateInvalid");
    }

    [Fact]
    public void DefaultRegistryDate_Fails()
    {
        var result = Validator.Validate(Valid() with { DepartmentRegistryDate = default });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void MovementWithoutDestination_Fails()
    {
        var result = Validator.Validate(
            Valid() with { Movement = new CreateDocumentMovementInput(Guid.Empty) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorCode == "Department.NotFound");
    }

    [Fact]
    public void MovementWithDestination_Passes()
    {
        var result = Validator.Validate(
            Valid() with { Movement = new CreateDocumentMovementInput(Guid.NewGuid()) });

        Assert.True(result.IsValid);
    }
}
