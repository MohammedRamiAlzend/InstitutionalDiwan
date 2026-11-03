using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Archiving.SavedSearches;

/// <summary>Keeps at most one default saved search per user.</summary>
internal static class SavedSearchDefaults
{
    /// <summary>
    /// Clears the user's current default unless it is <paramref name="keepSavedSearchId"/>. The change
    /// is saved at once, inside the command's transaction, because the filtered unique index would
    /// reject the new default if the database applied it first.
    /// </summary>
    public static async Task ClearCurrentAsync(
        ISavedSearchRepository repository,
        IUnitOfWork unitOfWork,
        Guid userId,
        Guid? keepSavedSearchId,
        CancellationToken cancellationToken)
    {
        var current = await repository.GetDefaultAsync(userId, cancellationToken);
        if (current is null || current.SavedSearchId == keepSavedSearchId)
            return;

        current.ClearDefault(unitOfWork.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
