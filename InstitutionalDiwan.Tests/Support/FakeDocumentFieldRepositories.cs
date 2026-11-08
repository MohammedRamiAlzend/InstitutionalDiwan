using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Tests.Support;

internal sealed class FakeDocumentFieldRepository : IDocumentFieldRepository
{
    public List<DocumentField> Fields { get; } = new();

    public Task<DocumentField?> GetByIdAsync(Guid documentFieldId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Fields.FirstOrDefault(field => field.DocumentFieldId == documentFieldId));

    public Task<DocumentField?> GetByKeyAsync(int documentTypeId, string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(Fields.FirstOrDefault(field => field.DocumentTypeId == documentTypeId && field.Key == key));

    public Task<IReadOnlyList<DocumentField>> ListByDocumentTypeAsync(
        int documentTypeId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DocumentField>>(
            Fields.Where(field => field.DocumentTypeId == documentTypeId).OrderBy(field => field.CreatedAt).ToList());

    public void Add(DocumentField field) => Fields.Add(field);
}

internal sealed class FakeDocumentTypeSchemaVersionRepository : IDocumentTypeSchemaVersionRepository
{
    public List<DocumentTypeSchemaVersion> Versions { get; } = new();

    public Task<DocumentTypeSchemaVersion?> GetByIdAsync(Guid schemaVersionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Versions.FirstOrDefault(version => version.SchemaVersionId == schemaVersionId));

    public Task<DocumentTypeSchemaVersion?> GetDraftAsync(int documentTypeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Versions.FirstOrDefault(version =>
            version.DocumentTypeId == documentTypeId && version.Status == DocumentTypeSchemaVersionStatus.Draft));

    public Task<DocumentTypeSchemaVersion?> GetPublishedAsync(int documentTypeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Versions
            .Where(version => version.DocumentTypeId == documentTypeId && version.Status == DocumentTypeSchemaVersionStatus.Published)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault());

    public Task<int> GetLatestVersionNumberAsync(int documentTypeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Versions.Where(version => version.DocumentTypeId == documentTypeId)
            .Select(version => version.VersionNumber).DefaultIfEmpty(0).Max());

    public Task<IReadOnlyList<DocumentTypeSchemaVersion>> ListByDocumentTypeAsync(
        int documentTypeId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DocumentTypeSchemaVersion>>(
            Versions.Where(version => version.DocumentTypeId == documentTypeId)
                .OrderByDescending(version => version.VersionNumber).ToList());

    public void Add(DocumentTypeSchemaVersion version) => Versions.Add(version);

    public void Remove(DocumentTypeSchemaVersion version) => Versions.Remove(version);
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
