using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

/// <summary>
/// Loads the single <see cref="InstitutionSetting"/> row. The table is a singleton by convention —
/// the seeder creates it and updates always apply to that row.
/// </summary>
internal sealed class InstitutionSettingRepository : IInstitutionSettingRepository
{
    private readonly DiwanDbContext _context;

    public InstitutionSettingRepository(DiwanDbContext context) => _context = context;

    public Task<InstitutionSetting?> GetSingletonAsync(CancellationToken cancellationToken = default) =>
        _context.InstitutionSettings
            .OrderBy(setting => setting.SettingId)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(InstitutionSetting setting) => _context.InstitutionSettings.Add(setting);
}
