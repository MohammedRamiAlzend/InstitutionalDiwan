using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Ui;

namespace InstitutionalDiwan.Application.Features.Ui.UiLabels;

public sealed class SetUiLabelOverrideCommandValidator : AbstractValidator<SetUiLabelOverrideCommand>
{
    public SetUiLabelOverrideCommandValidator()
    {
        RuleFor(command => command.Key)
            .Must(key => UiLabelOverride.NormalizeKey(key) is { } normalized &&
                System.Text.RegularExpressions.Regex.IsMatch(normalized, "^[a-z0-9]+([.\\-][a-z0-9]+)*$"))
            .WithErrorCode("UiLabel.KeyInvalid");

        RuleFor(command => command.Value)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithErrorCode("UiLabel.ValueEmpty")
            .MaximumLength(UiLabelOverride.MaxValueLength)
            .WithErrorCode("UiLabel.ValueTooLong");
    }
}