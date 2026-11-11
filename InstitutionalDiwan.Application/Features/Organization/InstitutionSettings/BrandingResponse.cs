namespace InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;

/// <summary>
/// What every screen shows to identify the institution: its name and whether it has an uploaded logo.
/// <paramref name="LogoUpdatedAt"/> is null without a logo and otherwise serves as the cache version of
/// the logo address.
/// </summary>
public sealed record BrandingResponse(string InstitutionNameAr, string? InstitutionNameEn, DateTime? LogoUpdatedAt);
