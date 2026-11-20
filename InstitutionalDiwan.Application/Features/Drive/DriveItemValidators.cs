using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Drive;

namespace InstitutionalDiwan.Application.Features.Drive;

public sealed class CreateDriveFolderCommandValidator : AbstractValidator<CreateDriveFolderCommand>
{
    public CreateDriveFolderCommandValidator()
    {
        RuleFor(command => command.SpaceId).NotEmpty().WithErrorCode("Drive.SpaceNotFound");
        RuleFor(command => command.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("Drive.ItemNameEmpty")
            .MaximumLength(DriveItem.MaxNameLength).WithErrorCode("Drive.ItemNameTooLong");
    }
}

public sealed class RenameDriveItemCommandValidator : AbstractValidator<RenameDriveItemCommand>
{
    public RenameDriveItemCommandValidator()
    {
        RuleFor(command => command.ItemId).NotEmpty().WithErrorCode("Drive.ItemNotFound");
        RuleFor(command => command.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("Drive.ItemNameEmpty")
            .MaximumLength(DriveItem.MaxNameLength).WithErrorCode("Drive.ItemNameTooLong");
    }
}

public sealed class MoveDriveItemCommandValidator : AbstractValidator<MoveDriveItemCommand>
{
    public MoveDriveItemCommandValidator()
    {
        RuleFor(command => command.ItemId).NotEmpty().WithErrorCode("Drive.ItemNotFound");
    }
}

public sealed class StarDriveItemCommandValidator : AbstractValidator<StarDriveItemCommand>
{
    public StarDriveItemCommandValidator()
    {
        RuleFor(command => command.ItemId).NotEmpty().WithErrorCode("Drive.ItemNotFound");
    }
}

public sealed class UnstarDriveItemCommandValidator : AbstractValidator<UnstarDriveItemCommand>
{
    public UnstarDriveItemCommandValidator()
    {
        RuleFor(command => command.ItemId).NotEmpty().WithErrorCode("Drive.ItemNotFound");
    }
}