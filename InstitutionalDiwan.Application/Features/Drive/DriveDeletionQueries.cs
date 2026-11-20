using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>
/// Lists deletion requests. <paramref name="MineOnly"/> returns the caller's own requests; otherwise
/// ADMIN sees all and a department head sees the requests they may review (filtered inside SQL).
/// </summary>
public sealed record ListDriveDeletionRequestsQuery(
    int Page = 1,
    int PageSize = 20,
    int? StatusId = null,
    bool MineOnly = false) : IQuery<PagedResult<DriveDeletionRequestResponse>>;

internal sealed class ListDriveDeletionRequestsQueryHandler
    : IQueryHandler<ListDriveDeletionRequestsQuery, PagedResult<DriveDeletionRequestResponse>>
{
    private readonly IDriveDeletionRequestRepository _requests;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;

    public ListDriveDeletionRequestsQueryHandler(
        IDriveDeletionRequestRepository requests,
        IDriveAccessEvaluator access,
        IUserContext userContext)
    {
        _requests = requests;
        _access = access;
        _userContext = userContext;
    }

    public async Task<Result<PagedResult<DriveDeletionRequestResponse>>> Handle(
        ListDriveDeletionRequestsQuery request, CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize);

        Guid? requestedBy = request.MineOnly ? _userContext.UserId : null;
        Guid? reviewer = !request.MineOnly && !_access.IsAdmin ? _userContext.UserId : null;

        var rows = await _requests.ListPagedAsync(page, request.StatusId, requestedBy, reviewer, cancellationToken);

        return Result.Success(new PagedResult<DriveDeletionRequestResponse>(
            rows.Items.Select(DriveDeletionRequestResponse.From).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            rows.TotalCount));
    }
}