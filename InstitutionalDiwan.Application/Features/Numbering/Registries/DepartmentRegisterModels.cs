using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

/// <summary>
/// The register book of one department: every document line written there, ordered by register
/// date. <paramref name="NumberingYear"/> is the year of the department's own date.
/// </summary>
public sealed record ListDepartmentRegisterQuery(Guid DepartmentId, int? NumberingYear)
    : IQuery<IReadOnlyCollection<DepartmentRegisterLineResponse>>;

/// <summary>
/// One document as it appears in a department's register. When the department renumbered the
/// document, the replaced line (<c>StatusId</c> 3) carries the number that took its place in
/// <c>ReplacedByNumberText</c> and the newer line carries the earlier number in <c>ReplacesNumberText</c>.
/// </summary>
public sealed record DepartmentRegisterLineResponse(
    Guid DocumentNumberId,
    Guid DocumentId,
    string DocumentTitle,
    string? CentralNumberText,
    string RegistryNumberText,
    int NumberingYear,
    DateTime? RegistryDate,
    int StatusId,
    int RegistrationCount,
    string? ReplacedByNumberText = null,
    string? ReplacesNumberText = null);
