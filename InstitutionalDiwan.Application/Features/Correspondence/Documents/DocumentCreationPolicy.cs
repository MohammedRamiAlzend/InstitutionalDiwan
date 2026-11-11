using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Who may create documents. The institution settings can reserve creation to the Central Diwan;
/// otherwise every department with the creation permission may create.
/// </summary>
internal static class DocumentCreationPolicy
{
    public static async Task<Result> EnsureMayCreateAsync(
        IInstitutionSettingRepository settingRepository,
        Guid creatorDepartmentId,
        CancellationToken cancellationToken)
    {
        var setting = await settingRepository.GetSingletonAsync(cancellationToken);
        return setting is null || setting.CanCreateDocuments(creatorDepartmentId)
            ? Result.Success()
            : Result.Failure(DomainErrors.Correspondence.DocumentCreationRestricted);
    }
}
