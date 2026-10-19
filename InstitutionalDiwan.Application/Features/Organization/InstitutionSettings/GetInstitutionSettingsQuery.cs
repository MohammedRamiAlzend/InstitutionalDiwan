using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;

/// <summary>Returns the singleton institution settings row.</summary>
public sealed record GetInstitutionSettingsQuery : IQuery<InstitutionSettingResponse>;
