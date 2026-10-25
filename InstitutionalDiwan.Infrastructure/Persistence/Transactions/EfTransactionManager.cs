using InstitutionalDiwan.Application.Abstractions.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace InstitutionalDiwan.Infrastructure.Persistence.Transactions;

/// <summary>Starts an explicit EF Core transaction, or joins the one already open.</summary>
internal sealed class EfTransactionManager : ITransactionManager
{
    private readonly DiwanDbContext _context;

    public EfTransactionManager(DiwanDbContext context) => _context = context;

    public async Task<ITransactionScope> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is not null)
            return NoOpTransactionScope.Instance;

        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new EfTransactionScope(transaction);
    }

    private sealed class EfTransactionScope : ITransactionScope
    {
        private readonly IDbContextTransaction _transaction;
        private bool _completed;

        public EfTransactionScope(IDbContextTransaction transaction) => _transaction = transaction;

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.CommitAsync(cancellationToken);
            _completed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_completed)
                await _transaction.RollbackAsync();

            await _transaction.DisposeAsync();
        }
    }

    private sealed class NoOpTransactionScope : ITransactionScope
    {
        public static readonly NoOpTransactionScope Instance = new();

        public Task CommitAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
