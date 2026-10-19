using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

/// <summary>
/// Updates a registry's scope and template. The format pattern is frozen once any number has been
/// issued from the registry, because already-issued numbers would no longer match it.
/// </summary>
public sealed record UpdateNumberingRegistryCommand(
    Guid RegistryId,
    string NameAr,
    string? FormatPattern = null,
    int ResetPolicyId = 0,
    Guid? DepartmentId = null,
    int? DocumentTypeId = null,
    int? DirectionId = null) : ICommand<NumberingRegistryResponse>;
