using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;

/// <summary>Shared rules for the settings of a field; type-specific rules are enforced by the domain.</summary>
public sealed class DocumentFieldSettingsInputValidator : AbstractValidator<DocumentFieldSettingsInput>
{
    public DocumentFieldSettingsInputValidator()
    {
        RuleFor(settings => settings.LabelAr)
            .Must(label => !string.IsNullOrWhiteSpace(label)).WithErrorCode("DocumentFields.LabelEmpty")
            .MaximumLength(200).WithErrorCode("DocumentFields.LabelTooLong");
    }
}

public sealed class CreateDocumentFieldCommandValidator : AbstractValidator<CreateDocumentFieldCommand>
{
    public CreateDocumentFieldCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty().WithErrorCode("Identity.UserIdEmpty");
        RuleFor(command => command.DocumentTypeId).GreaterThan(0).WithErrorCode("DocumentFields.DocumentTypeInvalid");
        RuleFor(command => command.Key)
            .Must(key => DocumentField.IsValidKey(key?.Trim())).WithErrorCode("DocumentFields.KeyInvalid");
        RuleFor(command => command.DataType)
            .IsInEnum().WithErrorCode("DocumentFields.DataTypeInvalid");
    }
}

public sealed class CreateSchemaDraftCommandValidator : AbstractValidator<CreateSchemaDraftCommand>
{
    public CreateSchemaDraftCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty().WithErrorCode("Identity.UserIdEmpty");
        RuleFor(command => command.DocumentTypeId).GreaterThan(0).WithErrorCode("DocumentFields.DocumentTypeInvalid");
    }
}

public sealed class AddFieldToDraftCommandValidator : AbstractValidator<AddFieldToDraftCommand>
{
    public AddFieldToDraftCommandValidator()
    {
        RuleFor(command => command.SchemaVersionId).NotEmpty().WithErrorCode("DocumentFields.SchemaVersionNotFound");
        RuleFor(command => command.DocumentFieldId).NotEmpty().WithErrorCode("DocumentFields.FieldNotFound");
        RuleFor(command => command.Settings).NotNull().SetValidator(new DocumentFieldSettingsInputValidator());
    }
}

public sealed class UpdateDraftFieldCommandValidator : AbstractValidator<UpdateDraftFieldCommand>
{
    public UpdateDraftFieldCommandValidator()
    {
        RuleFor(command => command.SchemaVersionId).NotEmpty().WithErrorCode("DocumentFields.SchemaVersionNotFound");
        RuleFor(command => command.DocumentFieldId).NotEmpty().WithErrorCode("DocumentFields.FieldNotFound");
        RuleFor(command => command.Settings).NotNull().SetValidator(new DocumentFieldSettingsInputValidator());
    }
}

public sealed class PublishSchemaVersionCommandValidator : AbstractValidator<PublishSchemaVersionCommand>
{
    public PublishSchemaVersionCommandValidator()
    {
        RuleFor(command => command.SchemaVersionId).NotEmpty().WithErrorCode("DocumentFields.SchemaVersionNotFound");
        RuleFor(command => command.UserId).NotEmpty().WithErrorCode("Identity.UserIdEmpty");
    }
}

public sealed class SetDocumentFieldValuesCommandValidator : AbstractValidator<SetDocumentFieldValuesCommand>
{
    public SetDocumentFieldValuesCommandValidator()
    {
        RuleFor(command => command.DocumentId).NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");
        RuleFor(command => command.Values)
            .NotNull()
            .Must(values => values is null || values.Count <= DocumentTypeSchemaVersion.MaxFields)
            .WithErrorCode("DocumentFields.TooManyFields");
        RuleForEach(command => command.Values)
            .Must(value => value is not null && !string.IsNullOrWhiteSpace(value.FieldKey))
            .WithErrorCode("DocumentFields.KeyInvalid");
    }
}
