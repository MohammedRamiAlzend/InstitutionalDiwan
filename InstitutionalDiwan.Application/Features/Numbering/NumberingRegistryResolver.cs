using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering;

/// <summary>
/// Picks the most specific active numbering registry for a department/document type/direction
/// scope, falling back to the shared default registry (created on first use) when none matches.
/// Shared by document registration and ad-hoc number issuance so both allocate identically.
/// </summary>
internal static class NumberingRegistryResolver
{
    public const string DefaultRegistryCode = "DEFAULT";

    public static async Task<Result<NumberingRegistry>> ResolveAsync(
        INumberingRegistryRepository registryRepository,
        Guid departmentId,
        int documentTypeId,
        int directionId,
        CancellationToken cancellationToken)
    {
        var registry = await registryRepository.GetActiveForScopeAsync(
            departmentId, documentTypeId, directionId, cancellationToken);

        if (registry is not null)
            return Result.Success(registry);

        var created = NumberingRegistry.Create(
            DefaultRegistryCode,
            "سجل الترقيم الافتراضي");

        if (created.IsFailure)
            return Result.Failure<NumberingRegistry>(created.Error);

        registryRepository.Add(created.Value);
        return Result.Success(created.Value);
    }
}
