using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;

public sealed class UpdateInstitutionSettingsCommandValidator
    : AbstractValidator<UpdateInstitutionSettingsCommand>
{
    public UpdateInstitutionSettingsCommandValidator()
    {
        RuleFor(command => command.InstitutionNameAr)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithErrorCode("InstitutionSetting.NameEmpty")
            .MaximumLength(200);

        RuleFor(command => command.InstitutionNameEn)
            .MaximumLength(200);

        RuleFor(command => command.NumberingModeId)
            .GreaterThan(0).WithErrorCode("InstitutionSetting.NumberingModeEmpty");

        RuleFor(command => command.NumberingResetPolicyId)
            .GreaterThan(0).WithErrorCode("InstitutionSetting.ResetPolicyEmpty");

        RuleFor(command => command.CentralDiwanDepartmentId)
            .NotEmpty().WithErrorCode("InstitutionSetting.CentralDiwanRequired");

        RuleFor(command => command.DefaultTimezone)
            .MaximumLength(100);

        RuleFor(command => command.CentralDiwanTransferReason)
            .MaximumLength(1000);
    }
}
