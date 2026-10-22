namespace InstitutionalDiwan.Domain.Common;

public interface IUnitOfWork
{
    /// <summary>
    /// The instant this unit of work stamps its writes with. Exposed here so every handler in one
    /// transaction shares the same "now" and tests can control it in one place.
    /// </summary>
    DateTime UtcNow { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
