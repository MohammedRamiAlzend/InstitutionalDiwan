using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>Lists trashed items the caller may reach (owner, space owner, ADMIN, or owner's department head).</summary>
public sealed record ListDriveTrashQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? SpaceId = null,
    string? Search = null) : IQuery<PagedResult<DriveItemResponse>>;

internal sealed class ListDriveTrashQueryHandler : IQueryHandler<ListDriveTrashQuery, PagedResult<DriveItemResponse>>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;

    public ListDriveTrashQueryHandler(
        IDriveItemRepository items,
        IDriveAccessEvaluator access,
        IUserContext userContext)
    {
        _items = items;
        _access = access;
        _userContext = userContext;
    }

    public async Task<Result<PagedResult<DriveItemResponse>>> Handle(ListDriveTrashQuery request, CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize, request.Search);
        var filter = new DriveItemFilter(SpaceId: request.SpaceId, Search: request.Search, Trashed: true);

        var departmentIds = await _access.GetCallerDepartmentIdsAsync(cancellationToken);
        var rows = await _items.ListPagedAsync(page, filter, _userContext.UserId, departmentIds, cancellationToken);

        return Result.Success(new PagedResult<DriveItemResponse>(
            rows.Items.Select(item => DriveItemResponse.From(item)).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            rows.TotalCount));
    }
}