using InstitutionalDiwan.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Base for the small reference-table seeders. Each seeded lookup exposes a fixed catalog of
/// stable ids + English/Arabic labels; seeding is idempotent and never overwrites existing rows.
/// </summary>
public abstract class LookupSeederBase<TEntity> : ISeeder
    where TEntity : LookupEntity
{
    public IReadOnlyCollection<string> Environments { get; } = new[] { SeedingEnvironments.All };

    public abstract int Order { get; }

    protected abstract IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; }

    protected abstract TEntity BuildItem(int id, string desc, string descAr);

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var set = context.Db.Set<TEntity>();

        var existingIds = (await set
                .Select(entity => entity.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var added = false;
        foreach (var (id, desc, descAr) in Catalog)
        {
            if (existingIds.Contains(id))
                continue;

            set.Add(BuildItem(id, desc, descAr));
            added = true;
        }

        if (added)
            await context.Db.SaveChangesAsync(cancellationToken);
    }
}
