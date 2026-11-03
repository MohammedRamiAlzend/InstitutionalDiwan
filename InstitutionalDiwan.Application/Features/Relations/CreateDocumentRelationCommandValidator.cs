using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Relations;

namespace InstitutionalDiwan.Application.Features.Relations;

public sealed class CreateDocumentRelationCommandValidator : AbstractValidator<CreateDocumentRelationCommand>
{
    public CreateDocumentRelationCommandValidator()
    {
        RuleFor(command => command.DocumentId)
            .NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");

        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.TargetDocumentId)
            .NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");

        RuleFor(command => command.RelationTypeId)
            .GreaterThan(0).WithErrorCode("Relations.RelationTypeNotFound");

        RuleFor(command => command.Notes)
            .MaximumLength(DocumentRelation.MaxNotesLength)
            .WithErrorCode("Relations.NotesTooLong");
    }
}
