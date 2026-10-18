using InstitutionalDiwan.Domain.Entities.Numbering;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

/// <summary>Read model returned by the numbering registry endpoints.</summary>
public sealed record NumberingRegistryResponse(
    Guid RegistryId,
    string Code,
    string NameAr,
    Guid? DepartmentId,
    string? DocumentTypeCode,
    string? Direction,
    string FormatPattern,
    string ResetPolicy,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyCollection<NumberingCounterResponse> Counters)
{
    public static NumberingRegistryResponse From(NumberingRegistry registry) => new(
        registry.RegistryId,
        registry.Code,
        registry.NameAr,
        registry.DepartmentId,
        registry.DocumentTypeCode,
        registry.Direction,
        registry.FormatPattern,
        registry.ResetPolicy,
        registry.IsActive,
        registry.CreatedAt,
        registry.Counters
            .OrderByDescending(counter => counter.NumberingYear)
            .Select(NumberingCounterResponse.From)
            .ToList());
}

/// <summary>Per-year sequence state attached to a numbering registry.</summary>
public sealed record NumberingCounterResponse(
    Guid CounterId,
    int NumberingYear,
    long LastValue,
    DateTime UpdatedAt)
{
    public static NumberingCounterResponse From(NumberingCounter counter) => new(
        counter.CounterId,
        counter.NumberingYear,
        counter.LastValue,
        counter.UpdatedAt);
}
