namespace InstitutionalDiwan.Application.Abstractions.Persistence;

/// <summary>
/// Opens an explicit database transaction around a command, so the aggregate change, its audit
/// entry, and any dependent rows commit or roll back together.
/// </summary>
public interface ITransactionManager
{
    Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>One unit of transactional work. Disposing without committing rolls the transaction back.</summary>
public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
