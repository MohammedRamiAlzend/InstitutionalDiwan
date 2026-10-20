using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

/// <summary>
/// The register book of one department: every document line written there, ordered by register
/// date. <paramref name="NumberingYear"/> is the year of the department's own date.
/// </summary>
public sealed record ListDepartmentRegisterQuery(Guid DepartmentId, int? NumberingYear)
    : IQuery<IReadOnlyCollection<DepartmentRegisterLineResponse>>;

/// <summary>One document as it appears in a department's register.</summary>
public sealed record DepartmentRegisterLineResponse(
    Guid DocumentNumberId,
    Guid DocumentId,
    string DocumentTitle,
    string? CentralNumberText,
    string RegistryNumberText,
    int NumberingYear,
    DateTime? RegistryDate,
    int StatusId,
    int RegistrationCount);
