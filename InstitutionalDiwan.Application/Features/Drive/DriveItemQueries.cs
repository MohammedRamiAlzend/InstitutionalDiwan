using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>Browses Drive items with access filtering applied inside the query (§3/14, §7.5).</summary>
public sealed record BrowseDriveItemsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? SpaceId = null,
    Guid? ParentId = null,
    string? Search = null,
    int? ItemKindId = null,
    Guid? OwnerUserId = null,
    bool Trashed = false,
    bool StarredOnly = false,
    DateTime? ModifiedFrom = null,
    DateTime? ModifiedTo = null,
    string? SortBy = null,
    bool SortDescending = false) : IQuery<PagedResult<DriveItemResponse>>;

/// <summary>Reads one item the caller can view.</summary>
public sealed record GetDriveItemQuery(Guid ItemId) : IQuery<DriveItemResponse>;

/// <summary>The ancestor path of an item, root first, ending with the item itself.</summary>
public sealed record GetDriveBreadcrumbQuery(Guid ItemId) : IQuery<IReadOnlyList<DriveBreadcrumbNode>>;

/// <summary>The direct children of a folder (or space root), folders first.</summary>
public sealed record GetDriveChildrenQuery(Guid SpaceId, Guid? ParentId) : IQuery<IReadOnlyList<DriveItemResponse>>;

internal sealed class BrowseDriveItemsQueryHandler : IQueryHandler<BrowseDriveItemsQuery, PagedResult<DriveItemResponse>>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveStarRepository _stars;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;

    public BrowseDriveItemsQueryHandler(
        IDriveItemRepository items,
        IDriveStarRepository stars,
        IDriveAccessEvaluator access,
        IUserContext userContext)
    {
        _items = items;
        _stars = stars;
        _access = access;
        _userContext = userContext;
    }

    public async Task<Result<PagedResult<DriveItemResponse>>> Handle(BrowseDriveItemsQuery request, CancellationToken cancellationToken)
    {
        var page = new PageRequest(
            request.Page, request.PageSize, request.Search, request.SortBy, request.SortDescending);
        var filter = new DriveItemFilter(
            request.SpaceId, request.ParentId, request.Search, request.ItemKindId, request.OwnerUserId,
            request.Trashed, request.StarredOnly, request.ModifiedFrom, request.ModifiedTo);

        var departmentIds = await _access.GetCallerDepartmentIdsAsync(cancellationToken);
        var rows = await _items.ListPagedAsync(page, filter, _userContext.UserId, departmentIds, cancellationToken);

        var starred = (await _stars.ListByUserAsync(_userContext.UserId, cancellationToken))
            .Select(star => star.ItemId).ToHashSet();

        return Result.Success(new PagedResult<DriveItemResponse>(
            rows.Items.Select(item => DriveItemResponse.From(item, starred.Contains(item.ItemId))).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            rows.TotalCount));
    }
}

internal sealed class GetDriveItemQueryHandler : IQueryHandler<GetDriveItemQuery, DriveItemResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveStarRepository _stars;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;

    public GetDriveItemQueryHandler(
        IDriveItemRepository items,
        IDriveStarRepository stars,
        IDriveAccessEvaluator access,
        IUserContext userContext)
    {
        _items = items;
        _stars = stars;
        _access = access;
        _userContext = userContext;
    }

    public async Task<Result<DriveItemResponse>> Handle(GetDriveItemQuery request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.ItemNotFound);
        if (!await _access.CanAccessItemAsync(request.ItemId, DriveRole.ViewerId, cancellationToken))
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.AccessDenied);

        var star = await _stars.FindAsync(_userContext.UserId, request.ItemId, cancellationToken);
        return Result.Success(DriveItemResponse.From(item, star is not null));
    }
}

internal sealed class GetDriveBreadcrumbQueryHandler : IQueryHandler<GetDriveBreadcrumbQuery, IReadOnlyList<DriveBreadcrumbNode>>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveAccessEvaluator _access;

    public GetDriveBreadcrumbQueryHandler(IDriveItemRepository items, IDriveAccessEvaluator access)
    {
        _items = items;
        _access = access;
    }

    public async Task<Result<IReadOnlyList<DriveBreadcrumbNode>>> Handle(GetDriveBreadcrumbQuery request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure<IReadOnlyList<DriveBreadcrumbNode>>(DomainErrors.Drive.ItemNotFound);
        if (!await _access.CanAccessItemAsync(request.ItemId, DriveRole.ViewerId, cancellationToken))
            return Result.Failure<IReadOnlyList<DriveBreadcrumbNode>>(DomainErrors.Drive.AccessDenied);

        var ancestorIds = await _items.GetAncestorIdsAsync(request.ItemId, cancellationToken);
        var nodes = new List<DriveBreadcrumbNode>();
        foreach (var ancestorId in ancestorIds.Reverse())
        {
            var ancestor = await _items.GetByIdAsync(ancestorId, cancellationToken);
            if (ancestor is not null)
                nodes.Add(new DriveBreadcrumbNode(ancestor.ItemId, ancestor.Name, ancestor.ItemKindId));
        }

        nodes.Add(new DriveBreadcrumbNode(item.ItemId, item.Name, item.ItemKindId));
        return Result.Success((IReadOnlyList<DriveBreadcrumbNode>)nodes);
    }
}

internal sealed class GetDriveChildrenQueryHandler : IQueryHandler<GetDriveChildrenQuery, IReadOnlyList<DriveItemResponse>>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveSpaceRepository _spaces;
    private readonly IDriveStarRepository _stars;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;

    public GetDriveChildrenQueryHandler(
        IDriveItemRepository items,
        IDriveSpaceRepository spaces,
        IDriveStarRepository stars,
        IDriveAccessEvaluator access,
        IUserContext userContext)
    {
        _items = items;
        _spaces = spaces;
        _stars = stars;
        _access = access;
        _userContext = userContext;
    }

    public async Task<Result<IReadOnlyList<DriveItemResponse>>> Handle(GetDriveChildrenQuery request, CancellationToken cancellationToken)
    {
        if (request.ParentId is { } parentId)
        {
            var parent = await _items.GetByIdAsync(parentId, cancellationToken);
            if (parent is null || parent.SpaceId != request.SpaceId)
                return Result.Failure<IReadOnlyList<DriveItemResponse>>(DomainErrors.Drive.ItemNotFound);
            if (!await _access.CanAccessItemAsync(parentId, DriveRole.ViewerId, cancellationToken))
                return Result.Failure<IReadOnlyList<DriveItemResponse>>(DomainErrors.Drive.AccessDenied);
        }
        else
        {
            var space = await _spaces.GetByIdAsync(request.SpaceId, cancellationToken);
            if (space is null)
                return Result.Failure<IReadOnlyList<DriveItemResponse>>(DomainErrors.Drive.SpaceNotFound);
            if (space.OwnerUserId != _userContext.UserId && !_access.IsAdmin)
                return Result.Failure<IReadOnlyList<DriveItemResponse>>(DomainErrors.Drive.AccessDenied);
        }

        var children = await _items.GetChildrenAsync(request.SpaceId, request.ParentId, cancellationToken);
        var starred = (await _stars.ListByUserAsync(_userContext.UserId, cancellationToken))
            .Select(star => star.ItemId).ToHashSet();

        return Result.Success((IReadOnlyList<DriveItemResponse>)children
            .Select(item => DriveItemResponse.From(item, starred.Contains(item.ItemId)))
            .ToList());
    }
}