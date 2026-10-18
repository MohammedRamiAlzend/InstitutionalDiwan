using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Identity.AccessGrants;

public sealed class GrantDocumentAccessCommandValidator : AbstractValidator<GrantDocumentAccessCommand>
{
    public GrantDocumentAccessCommandValidator()
    {
        RuleFor(command => command.DocumentId)
            .NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");

        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserNotFound");

        RuleFor(command => command.GrantedByUserId)
            .NotEmpty().WithErrorCode("Identity.UserNotFound");

        RuleFor(command => command.AccessLevelId)
            .GreaterThan(0).WithErrorCode("Identity.AccessLevelInvalid");

        RuleFor(command => command.Reason).MaximumLength(500);
    }
}
