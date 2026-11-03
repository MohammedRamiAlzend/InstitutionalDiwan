using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Tests.Support;

internal sealed class FakeDocumentArchiveRepository : IDocumentArchiveRepository
{
    public List<DocumentArchiveEvent> Events { get; } = new();
    public List<ArchivedDocumentRow> Rows { get; } = new();
    public ArchiveSearchFilter? LastFilter { get; private set; }

    public Task<IReadOnlyList<DocumentArchiveEvent>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DocumentArchiveEvent>>(
            Events.Where(archiveEvent => archiveEvent.DocumentId == documentId).ToList());

    public Task<PagedList<ArchivedDocumentRow>> SearchAsync(
        ArchiveSearchFilter filter,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        LastFilter = filter;
        return Task.FromResult(new PagedList<ArchivedDocumentRow>(Rows, Rows.Count));
    }

    public void Add(DocumentArchiveEvent archiveEvent) => Events.Add(archiveEvent);
}
