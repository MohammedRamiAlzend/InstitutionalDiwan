using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Drive;

namespace InstitutionalDiwan.Application.Features.Drive;

public sealed class InitiateDriveUploadCommandValidator : AbstractValidator<InitiateDriveUploadCommand>
{
    public InitiateDriveUploadCommandValidator()
    {
        RuleFor(command => command.FileName)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("Drive.ItemNameEmpty")
            .MaximumLength(DriveItem.MaxNameLength).WithErrorCode("Drive.ItemNameTooLong");

        RuleFor(command => command.DeclaredSizeBytes)
            .GreaterThan(0).WithErrorCode("Drive.VersionSizeInvalid");

        RuleFor(command => command.SpaceId)
            .NotEmpty().When(command => command.FileItemId is null)
            .WithErrorCode("Drive.SpaceNotFound");
    }
}

public sealed class CompleteDriveUploadCommandValidator : AbstractValidator<CompleteDriveUploadCommand>
{
    public CompleteDriveUploadCommandValidator()
    {
        RuleFor(command => command.VersionId).NotEmpty().WithErrorCode("Drive.VersionNotFound");

        RuleFor(command => command.ChecksumSha256)
            .Length(64).When(command => !string.IsNullOrWhiteSpace(command.ChecksumSha256))
            .WithErrorCode("Drive.ChecksumInvalid");
    }
}

public sealed class CreateDriveFileCommandValidator : AbstractValidator<CreateDriveFileCommand>
{
    public CreateDriveFileCommandValidator()
    {
        RuleFor(command => command.FileName)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("Drive.ItemNameEmpty")
            .MaximumLength(DriveItem.MaxNameLength).WithErrorCode("Drive.ItemNameTooLong");

        RuleFor(command => command.SpaceId)
            .NotEmpty().WithErrorCode("Drive.SpaceNotFound");
    }
}

public sealed class SaveDriveTextContentCommandValidator : AbstractValidator<SaveDriveTextContentCommand>
{
    public SaveDriveTextContentCommandValidator()
    {
        RuleFor(command => command.ItemId).NotEmpty().WithErrorCode("Drive.ItemNotFound");
    }
}

public sealed class AbandonDriveUploadCommandValidator : AbstractValidator<AbandonDriveUploadCommand>
{
    public AbandonDriveUploadCommandValidator()
    {
        RuleFor(command => command.VersionId).NotEmpty().WithErrorCode("Drive.VersionNotFound");
    }
}