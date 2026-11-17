using InstitutionalDiwan.Domain.Entities.Ui;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

/// <summary>Loads and stores the superadmin UI label replacements (one row per overridden key).</summary>
internal sealed class UiLabelOverrideRepository : IUiLabelOverrideRepository
{
    private readonly DiwanDbContext _context;

    public UiLabelOverrideRepository(DiwanDbContext context) => _context = context;

    public async Task<IReadOnlyList<UiLabelOverride>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _context.UiLabelOverrides
            .OrderBy(labelOverride => labelOverride.Key)
            .ToListAsync(cancellationToken);

    public Task<UiLabelOverride?> GetByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        _context.UiLabelOverrides.FirstOrDefaultAsync(labelOverride => labelOverride.Key == key, cancellationToken);

    public void Add(UiLabelOverride labelOverride) => _context.UiLabelOverrides.Add(labelOverride);

    public void Remove(UiLabelOverride labelOverride) => _context.UiLabelOverrides.Remove(labelOverride);
}