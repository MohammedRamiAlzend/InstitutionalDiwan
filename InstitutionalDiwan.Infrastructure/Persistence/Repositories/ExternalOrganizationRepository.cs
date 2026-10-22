using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class ExternalOrganizationRepository : IExternalOrganizationRepository
{
    private readonly DiwanDbContext _context;

    public ExternalOrganizationRepository(DiwanDbContext context) => _context = context;

    public Task<ExternalOrganization?> GetByIdAsync(
        Guid externalOrganizationId,
        CancellationToken cancellationToken = default) =>
        _context.ExternalOrganizations
            .FirstOrDefaultAsync(
                organization => organization.ExternalOrganizationId == externalOrganizationId,
                cancellationToken);

    public Task<ExternalOrganization?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return _context.ExternalOrganizations
            .FirstOrDefaultAsync(organization => organization.Code == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalOrganization>> ListAllAsync(
        CancellationToken cancellationToken = default) =>
        await _context.ExternalOrganizations
            .OrderBy(organization => organization.Code)
            .ToListAsync(cancellationToken);

    public async Task<PagedList<ExternalOrganization>> ListPagedAsync(
        PageRequest page,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ExternalOrganizations.AsNoTracking();

        if (!includeInactive)
            query = query.Where(organization => organization.IsActive);

        if (page.NormalizedSearch is { } search)
        {
            var pattern = $"%{search}%";
            query = query.Where(organization =>
                EF.Functions.Like(organization.Code, pattern) ||
                EF.Functions.Like(organization.NameAr, pattern) ||
                (organization.NameEn != null && EF.Functions.Like(organization.NameEn, pattern)));
        }

        query = query.OrderBy(organization => organization.Code);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(cancellationToken);

        return new PagedList<ExternalOrganization>(items, totalCount);
    }

    public void Add(ExternalOrganization externalOrganization) =>
        _context.ExternalOrganizations.Add(externalOrganization);
}
