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

    public void Add(ExternalOrganization externalOrganization) =>
        _context.ExternalOrganizations.Add(externalOrganization);
}
