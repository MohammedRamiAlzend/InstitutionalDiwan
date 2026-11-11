using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>In-memory fields; a null department is the creation form, as in the real repository.</summary>
internal sealed class FakeDocumentFieldRepository : IDocumentFieldRepository
{
    public List<DocumentField> Fields { get; } = new();

    public Task<DocumentField?> GetByIdAsync(Guid documentFieldId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Fields.FirstOrDefault(field => field.DocumentFieldId == documentFieldId));

    public Task<DocumentField?> GetByKeyAsync(int documentTypeId, string key, CancellationToken cancellationToken = default) =>
        GetByKeyAsync(documentTypeId, null, key, cancellationToken);

    public Task<DocumentField?> GetByKeyAsync(int documentTypeId, Guid? departmentId, string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(Fields.FirstOrDefault(field =>
            field.DocumentTypeId == documentTypeId && field.DepartmentId == departmentId && field.Key == key));

    public Task<IReadOnlyList<DocumentField>> ListByDocumentTypeAsync(
        int documentTypeId,
        CancellationToken cancellationToken = default) =>
        ListByDocumentTypeAsync(documentTypeId, null, cancellationToken);

    public Task<IReadOnlyList<DocumentField>> ListByDocumentTypeAsync(
        int documentTypeId,
        Guid? departmentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DocumentField>>(
            Fields.Where(field => field.DocumentTypeId == documentTypeId && field.DepartmentId == departmentId)
                .OrderBy(field => field.CreatedAt).ToList());

    public void Add(DocumentField field) => Fields.Add(field);
}

/// <summary>In-memory schema versions; a null department is the creation form, as in the real repository.</summary>
internal sealed class FakeDocumentTypeSchemaVersionRepository : IDocumentTypeSchemaVersionRepository
{
    public List<DocumentTypeSchemaVersion> Versions { get; } = new();

    public Task<DocumentTypeSchemaVersion?> GetByIdAsync(Guid schemaVersionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Versions.FirstOrDefault(version => version.SchemaVersionId == schemaVersionId));

    public Task<DocumentTypeSchemaVersion?> GetDraftAsync(int documentTypeId, CancellationToken cancellationToken = default) =>
        GetDraftAsync(documentTypeId, null, cancellationToken);

    public Task<DocumentTypeSchemaVersion?> GetDraftAsync(int documentTypeId, Guid? departmentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(OfForm(documentTypeId, departmentId)
            .FirstOrDefault(version => version.Status == DocumentTypeSchemaVersionStatus.Draft));

    public Task<DocumentTypeSchemaVersion?> GetPublishedAsync(int documentTypeId, CancellationToken cancellationToken = default) =>
        GetPublishedAsync(documentTypeId, null, cancellationToken);

    public Task<DocumentTypeSchemaVersion?> GetPublishedAsync(int documentTypeId, Guid? departmentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(OfForm(documentTypeId, departmentId)
            .Where(version => version.Status == DocumentTypeSchemaVersionStatus.Published)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault());

    public Task<int> GetLatestVersionNumberAsync(int documentTypeId, CancellationToken cancellationToken = default) =>
        GetLatestVersionNumberAsync(documentTypeId, null, cancellationToken);

    public Task<int> GetLatestVersionNumberAsync(int documentTypeId, Guid? departmentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(OfForm(documentTypeId, departmentId)
            .Select(version => version.VersionNumber).DefaultIfEmpty(0).Max());

    public Task<IReadOnlyList<DocumentTypeSchemaVersion>> ListByDocumentTypeAsync(
        int documentTypeId,
        CancellationToken cancellationToken = default) =>
        ListByDocumentTypeAsync(documentTypeId, null, cancellationToken);

    public Task<IReadOnlyList<DocumentTypeSchemaVersion>> ListByDocumentTypeAsync(
        int documentTypeId,
        Guid? departmentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DocumentTypeSchemaVersion>>(
            OfForm(documentTypeId, departmentId).OrderByDescending(version => version.VersionNumber).ToList());

    public Task<IReadOnlyList<DocumentTypeSchemaVersion>> ListIssuedByDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DocumentTypeSchemaVersion>>(
            Versions.Where(version => version.DepartmentId == departmentId &&
                                      version.Status != DocumentTypeSchemaVersionStatus.Draft)
                .OrderBy(version => version.DocumentTypeId)
                .ThenByDescending(version => version.VersionNumber)
                .ToList());

    public void Add(DocumentTypeSchemaVersion version) => Versions.Add(version);

    public void Remove(DocumentTypeSchemaVersion version) => Versions.Remove(version);

    private IEnumerable<DocumentTypeSchemaVersion> OfForm(int documentTypeId, Guid? departmentId) =>
        Versions.Where(version => version.DocumentTypeId == documentTypeId && version.DepartmentId == departmentId);
}

internal sealed class FakeDocumentFieldOptionResolver
    : InstitutionalDiwan.Application.Features.Correspondence.DocumentFields.IDocumentFieldOptionResolver
{
    public Dictionary<Guid, IReadOnlyList<DocumentFieldOption>> Options { get; } = new();

    public Result EnsureSourceSupported(DocumentFieldBehavior behavior) =>
        behavior.OptionSource == DocumentFieldOptionSource.Lookup && behavior.LookupCatalog == "unknown"
            ? Result.Failure(DomainErrors.DocumentFields.OptionSourceInvalid)
            : Result.Success();

    public Task<IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>> ResolveAsync(
        DocumentTypeSchemaVersion schema,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>>(Options);
}
