namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

/// <summary>
/// A document's numbering picture: the central Diwan number plus one line per department register
/// it was written into, each with every dated registration event.
/// </summary>
public sealed record DocumentRegistryNumbersResponse(
    Guid DocumentId,
    Guid? CentralNumberId,
    string? CentralNumberText,
    DateTime? CentralRegistrationDate,
    IReadOnlyCollection<DepartmentRegistryNumberResponse> DepartmentNumbers);

/// <summary>
/// One department register line. The number is stable; the dates live in the history. A department
/// that renumbered the document has several lines: the replaced one (<c>StatusId</c> 3) names the
/// line that took its place in <c>ReplacedByNumberId</c>/<c>ReplacedByNumberText</c>, and the newer
/// one names the line it replaced in <c>ReplacesNumberId</c>/<c>ReplacesNumberText</c>.
/// </summary>
public sealed record DepartmentRegistryNumberResponse(
    Guid DocumentNumberId,
    Guid DepartmentId,
    Guid RegistryId,
    string RegistryNumberText,
    int NumberingYear,
    DateTime? RegistryDate,
    int StatusId,
    IReadOnlyCollection<DepartmentRegistrationEventResponse> Registrations,
    IReadOnlyCollection<DepartmentRegistryVisitResponse> Visits,
    Guid? ReplacedByNumberId = null,
    string? ReplacedByNumberText = null,
    Guid? ReplacesNumberId = null,
    string? ReplacesNumberText = null);

/// <summary>One dated arrival on a department register line, and the movement step that used it.</summary>
public sealed record DepartmentRegistryVisitResponse(
    Guid VisitId,
    DateTime RegistryDate,
    Guid? MovementStepId,
    Guid RecordedByUserId,
    DateTime RecordedAt,
    string? Notes);

/// <summary>A single visit to a department register, dated the way that department dated it.</summary>
public sealed record DepartmentRegistrationEventResponse(
    Guid HistoryId,
    int EventTypeId,
    DateTime? EffectiveDate,
    DateTime OccurredAt,
    Guid ActorUserId,
    string? Notes);
