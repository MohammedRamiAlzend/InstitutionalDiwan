using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Enforces the mandatory Central Diwan hub: a document moving between two departments must have
/// the Central Diwan on at least one side of the hop. A transfer between two ordinary departments
/// is rejected and must be split into two movements through the Central Diwan (for example
/// source → Central Diwan, then Central Diwan → destination).
/// </summary>
internal sealed class CentralDiwanRouting
{
    private readonly IInstitutionSettingRepository _settingRepository;

    public CentralDiwanRouting(IInstitutionSettingRepository settingRepository)
    {
        _settingRepository = settingRepository;
    }

    /// <summary>
    /// Returns the configured Central Diwan department, or a conflict when it has not been set.
    /// </summary>
    public async Task<Result<Guid>> GetCentralDiwanDepartmentIdAsync(
        CancellationToken cancellationToken)
    {
        var setting = await _settingRepository.GetSingletonAsync(cancellationToken);
        if (setting?.CentralDiwanDepartmentId is not { } centralDiwanDepartmentId ||
            centralDiwanDepartmentId == Guid.Empty)
        {
            return Result.Failure<Guid>(DomainErrors.Movements.CentralDiwanNotConfigured);
        }

        return Result.Success(centralDiwanDepartmentId);
    }

    /// <summary>
    /// Allows same-department routing (hand-off to an assignee) and any hop that starts or ends at
    /// the Central Diwan. Every other department pair is refused.
    /// </summary>
    public async Task<Result> ValidateRouteAsync(
        Guid sourceDepartmentId,
        Guid destinationDepartmentId,
        CancellationToken cancellationToken)
    {
        if (sourceDepartmentId == destinationDepartmentId)
            return Result.Success();

        var centralResult = await GetCentralDiwanDepartmentIdAsync(cancellationToken);
        if (centralResult.IsFailure)
            return Result.Failure(centralResult.Error);

        var centralDiwanDepartmentId = centralResult.Value;
        if (sourceDepartmentId == centralDiwanDepartmentId ||
            destinationDepartmentId == centralDiwanDepartmentId)
        {
            return Result.Success();
        }

        return Result.Failure(DomainErrors.Movements.CentralDiwanRoutingRequired);
    }
}
