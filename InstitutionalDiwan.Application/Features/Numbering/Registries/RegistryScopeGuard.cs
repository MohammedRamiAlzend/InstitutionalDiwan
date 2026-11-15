using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

/// <summary>
/// Applies the caller's department scope to numbering-registry reads and writes. A null
/// <paramref name="accessible"/> set means an institution-wide scope (no restriction); global
/// registries (no department) are visible to every department but only writable institution-wide.
/// </summary>
internal static class RegistryScopeGuard
{
    /// <summary>
    /// Decides whether the caller may modify a registry. Null means allowed.
    /// <c>RegistryNotFound</c> hides a registry the caller cannot see at all;
    /// <c>RegistryOutOfScope</c> rejects one they can see (global registry) but not touch.
    /// </summary>
    public static Error? CheckModify(Guid? registryDepartmentId, IReadOnlySet<Guid>? accessible)
    {
        if (accessible is null)
            return null;

        if (registryDepartmentId is null)
            return DomainErrors.Numbering.RegistryOutOfScope;

        return accessible.Contains(registryDepartmentId.Value)
            ? null
            : DomainErrors.Numbering.RegistryNotFound;
    }

    /// <summary>Decides whether the caller may place a registry in a target department. Null means allowed.</summary>
    public static Error? CheckTarget(Guid? targetDepartmentId, IReadOnlySet<Guid>? accessible)
    {
        if (accessible is null)
            return null;

        return targetDepartmentId is not null && accessible.Contains(targetDepartmentId.Value)
            ? null
            : DomainErrors.Numbering.RegistryOutOfScope;
    }

    public static bool CanRead(Guid? registryDepartmentId, IReadOnlySet<Guid>? accessible) =>
        accessible is null ||
        registryDepartmentId is null ||
        accessible.Contains(registryDepartmentId.Value);
}
