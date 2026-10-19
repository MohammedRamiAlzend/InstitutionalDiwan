using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

/// <summary>
/// Creates a numbering registry. Narrower registries (department, document type, direction) win over
/// broader ones when a document is registered; the code must be unique.
/// </summary>
public sealed record CreateNumberingRegistryCommand(
    string Code,
    string NameAr,
    string? FormatPattern = null,
    int ResetPolicyId = 0,
    Guid? DepartmentId = null,
    int? DocumentTypeId = null,
    int? DirectionId = null) : ICommand<NumberingRegistryResponse>;
