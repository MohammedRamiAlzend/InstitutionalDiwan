using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class SavedSearchRepository : ISavedSearchRepository
{
    private readonly DiwanDbContext _context;

    public SavedSearchRepository(DiwanDbContext context) => _context = context;

    public Task<SavedSearch?> GetByIdAsync(Guid savedSearchId, CancellationToken cancellationToken = default) =>
        _context.SavedSearches.FirstOrDefaultAsync(search => search.SavedSearchId == savedSearchId, cancellationToken);

    public async Task<IReadOnlyList<SavedSearch>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await _context.SavedSearches
            .AsNoTracking()
            .Where(search => search.UserId == userId)
            .OrderBy(search => search.Name)
            .ToListAsync(cancellationToken);

    public Task<SavedSearch?> GetDefaultAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.SavedSearches.FirstOrDefaultAsync(
            search => search.UserId == userId && search.IsDefault, cancellationToken);

    public Task<bool> ExistsByNameAsync(
        Guid userId,
        string name,
        Guid? excludingSavedSearchId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim();
        return _context.SavedSearches.AnyAsync(
            search => search.UserId == userId &&
                      search.Name == normalized &&
                      search.SavedSearchId != excludingSavedSearchId,
            cancellationToken);
    }

    public Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.SavedSearches.CountAsync(search => search.UserId == userId, cancellationToken);

    public void Add(SavedSearch savedSearch) => _context.SavedSearches.Add(savedSearch);

    public void Remove(SavedSearch savedSearch) => _context.SavedSearches.Remove(savedSearch);
}
