using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Drive;

namespace InstitutionalDiwan.Application.Features.Drive;

public sealed class CreateDriveSpaceCommandValidator : AbstractValidator<CreateDriveSpaceCommand>
{
    public CreateDriveSpaceCommandValidator()
    {
        RuleFor(command => command.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("Drive.SpaceNameEmpty")
            .MaximumLength(DriveSpace.MaxNameLength).WithErrorCode("Drive.SpaceNameTooLong");

        RuleFor(command => command.QuotaBytes)
            .GreaterThan(0).When(command => command.QuotaBytes is not null)
            .WithErrorCode("Drive.QuotaInvalid");
    }
}

public sealed class RenameDriveSpaceCommandValidator : AbstractValidator<RenameDriveSpaceCommand>
{
    public RenameDriveSpaceCommandValidator()
    {
        RuleFor(command => command.SpaceId).NotEmpty().WithErrorCode("Drive.SpaceNotFound");

        RuleFor(command => command.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("Drive.SpaceNameEmpty")
            .MaximumLength(DriveSpace.MaxNameLength).WithErrorCode("Drive.SpaceNameTooLong");
    }
}

public sealed class SetDriveSpaceQuotaCommandValidator : AbstractValidator<SetDriveSpaceQuotaCommand>
{
    public SetDriveSpaceQuotaCommandValidator()
    {
        RuleFor(command => command.SpaceId).NotEmpty().WithErrorCode("Drive.SpaceNotFound");

        RuleFor(command => command.QuotaBytes)
            .GreaterThan(0).When(command => command.QuotaBytes is not null)
            .WithErrorCode("Drive.QuotaInvalid");
    }
}