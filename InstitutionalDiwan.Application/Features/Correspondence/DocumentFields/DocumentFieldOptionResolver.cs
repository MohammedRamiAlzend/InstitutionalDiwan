using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;

/// <summary>
/// Reads the current choices of select fields fed by a source (departments, external organizations,
/// a lookup catalog). The choices are looked up on demand, so a schema version stays frozen while the
/// list it points at keeps up with the organization.
/// </summary>
public interface IDocumentFieldOptionResolver
{
    /// <summary>
    /// The choices of every source-fed field of <paramref name="schema"/>, by field id, deactivated
    /// entries included (flagged) so values already stored stay readable. Empty when none is source-fed.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>> ResolveAsync(
        DocumentTypeSchemaVersion schema,
        CancellationToken cancellationToken);

    /// <summary>Fails when a settings' lookup catalog is not one this system knows.</summary>
    Result EnsureSourceSupported(DocumentFieldBehavior behavior);
}

internal sealed class DocumentFieldOptionResolver : IDocumentFieldOptionResolver
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IExternalOrganizationRepository _organizationRepository;
    private readonly ILookupRepository _lookupRepository;

    public DocumentFieldOptionResolver(
        IDepartmentRepository departmentRepository,
        IExternalOrganizationRepository organizationRepository,
        ILookupRepository lookupRepository)
    {
        _departmentRepository = departmentRepository;
        _organizationRepository = organizationRepository;
        _lookupRepository = lookupRepository;
    }

    public Result EnsureSourceSupported(DocumentFieldBehavior behavior) =>
        behavior.OptionSource == DocumentFieldOptionSource.Lookup
        && !_lookupRepository.IsSupported(behavior.LookupCatalog ?? string.Empty)
            ? Result.Failure(DomainErrors.DocumentFields.OptionSourceInvalid)
            : Result.Success();

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>> ResolveAsync(
        DocumentTypeSchemaVersion schema,
        CancellationToken cancellationToken)
    {
        var resolved = new Dictionary<Guid, IReadOnlyList<DocumentFieldOption>>();
        // Two fields on the same source read it once.
        var bySource = new Dictionary<(DocumentFieldOptionSource, string?), IReadOnlyList<DocumentFieldOption>>();

        foreach (var field in schema.Fields.Where(f => f.Behavior.OptionSource != DocumentFieldOptionSource.Static))
        {
            var source = (field.Behavior.OptionSource, field.Behavior.LookupCatalog);
            if (!bySource.TryGetValue(source, out var options))
            {
                options = await ReadAsync(field.Behavior, cancellationToken);
                bySource[source] = options;
            }

            resolved[field.DocumentFieldId] = options;
        }

        return resolved;
    }

    private async Task<IReadOnlyList<DocumentFieldOption>> ReadAsync(
        DocumentFieldBehavior behavior,
        CancellationToken cancellationToken)
    {
        switch (behavior.OptionSource)
        {
            case DocumentFieldOptionSource.Departments:
                return (await _departmentRepository.ListAllAsync(cancellationToken))
                    .Select(department => new DocumentFieldOption(
                        department.DepartmentId.ToString(), department.NameAr, department.IsActive))
                    .OrderBy(option => option.LabelAr, StringComparer.Ordinal)
                    .ToList();

            case DocumentFieldOptionSource.ExternalOrganizations:
                return (await _organizationRepository.ListAllAsync(cancellationToken))
                    .Select(organization => new DocumentFieldOption(
                        organization.ExternalOrganizationId.ToString(), organization.NameAr, organization.IsActive))
                    .OrderBy(option => option.LabelAr, StringComparer.Ordinal)
                    .ToList();

            case DocumentFieldOptionSource.Lookup when
                behavior.LookupCatalog is { } catalog && _lookupRepository.IsSupported(catalog):
                return (await _lookupRepository.ListAsync(catalog, includeInactive: true, cancellationToken))
                    .Select(entry => new DocumentFieldOption(entry.Id.ToString(), entry.DescAr, entry.IsActive))
                    .ToList();

            default:
                return Array.Empty<DocumentFieldOption>();
        }
    }
}
