using InstitutionalDiwan.Domain.Entities.Decisions;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DecisionTypeRepository : IDecisionTypeRepository
{
    private readonly DiwanDbContext _context;

    public DecisionTypeRepository(DiwanDbContext context) => _context = context;

    public Task<DecisionType?> GetByIdAsync(Guid decisionTypeId, CancellationToken cancellationToken = default) =>
        _context.DecisionTypes
            .Include(type => type.DocumentTypes)
            .FirstOrDefaultAsync(type => type.DecisionTypeId == decisionTypeId, cancellationToken);

    public async Task<IReadOnlyList<DecisionType>> ListAsync(
        int? documentTypeId,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DecisionTypes.AsNoTracking().Include(type => type.DocumentTypes).AsQueryable();
        if (documentTypeId is { } typeId)
            query = query.Where(type => type.DocumentTypes.Any(link => link.DocumentTypeId == typeId));
        if (isActive is { } active)
            query = query.Where(type => type.IsActive == active);

        return await query.OrderBy(type => type.NameAr).ToListAsync(cancellationToken);
    }

    public void Add(DecisionType decisionType) => _context.DecisionTypes.Add(decisionType);
}
