using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Application.Features.Lookups;

/// <summary>Read model shared by every reference-lookup endpoint.</summary>
public sealed record LookupEntryResponse(
    int Id,
    string Desc,
    string DescAr,
    bool IsActive,
    DateTime CreatedAt)
{
    public static LookupEntryResponse From(LookupEntity entry) => new(
        entry.Id,
        entry.Desc,
        entry.DescAr,
        entry.IsActive,
        entry.CreatedAt);
}
