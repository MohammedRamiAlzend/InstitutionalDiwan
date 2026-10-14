using System.Threading;
using System.Threading.Tasks;

namespace InstitutionalDiwan.Domain.Common;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
