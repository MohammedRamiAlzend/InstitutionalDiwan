using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

/// <summary>
/// Recursive-CTE helpers over the Drive item tree. SQL Server and SQLite both support recursive
/// CTEs but only SQLite wants the RECURSIVE keyword, so the prefix is chosen per provider. The
/// domain caps the tree at 30 levels, well under SQL Server's default MAXRECURSION of 100.
/// </summary>
internal static class DriveTreeQuery
{
    /// <summary>Ancestor ids of the item, ordered from the parent up to the root.</summary>
    public static async Task<IReadOnlyList<Guid>> GetAncestorIdsAsync(
        this DiwanDbContext context,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var sql = $$"""
            WITH {{RecursiveKeyword(context)}}ancestors AS (
                SELECT ParentId AS Id, 1 AS Lvl FROM DRIVE_ITEMS WHERE ItemId = {0}
                UNION ALL
                SELECT i.ParentId, a.Lvl + 1
                FROM DRIVE_ITEMS i JOIN ancestors a ON i.ItemId = a.Id
                WHERE a.Id IS NOT NULL
            )
            SELECT Id FROM ancestors WHERE Id IS NOT NULL ORDER BY Lvl
            """;

        return await context.Database
            .SqlQueryRaw<Guid>(sql, itemId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>The deepest level present in the item tree (roots are level 1); 0 when empty.</summary>
    public static async Task<int> GetMaxDepthAsync(this DiwanDbContext context, CancellationToken cancellationToken)
    {
        var sql = $$"""
            WITH {{RecursiveKeyword(context)}}tree AS (
                SELECT ItemId, 1 AS Depth FROM DRIVE_ITEMS WHERE ParentId IS NULL
                UNION ALL
                SELECT i.ItemId, t.Depth + 1
                FROM DRIVE_ITEMS i JOIN tree t ON i.ParentId = t.ItemId
            )
            SELECT MAX(Depth) AS Value FROM tree
            """;

        var depths = await context.Database.SqlQueryRaw<int?>(sql).ToListAsync(cancellationToken);
        return depths.FirstOrDefault() ?? 0;
    }

    private static string RecursiveKeyword(DiwanDbContext context) =>
        context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite" ? "RECURSIVE " : string.Empty;
}