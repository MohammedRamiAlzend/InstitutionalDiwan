using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>Reads one space the caller owns, manages, or holds a grant on.</summary>
public sealed record GetDriveSpaceQuery(Guid SpaceId) : IQuery<DriveSpaceResponse>;

/// <summary>Lists the caller's personal space and the shared spaces they can reach.</summary>
public sealed record ListDriveSpacesQuery : IQuery<DriveSpacesOverviewResponse>;

internal sealed class GetDriveSpaceQueryHandler : IQueryHandler<GetDriveSpaceQuery, DriveSpaceResponse>
{
    private readonly IDriveSpaceRepository _spaces;
    private readonly IInstitutionSettingRepository _settings;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;

    public GetDriveSpaceQueryHandler(
        IDriveSpaceRepository spaces,
        IInstitutionSettingRepository settings,
        IDriveAccessEvaluator access,
        IUserContext userContext)
    {
        _spaces = spaces;
        _settings = settings;
        _access = access;
        _userContext = userContext;
    }

    public async Task<Result<DriveSpaceResponse>> Handle(GetDriveSpaceQuery request, CancellationToken cancellationToken)
    {
        var space = await _spaces.GetByIdAsync(request.SpaceId, cancellationToken);
        if (space is null)
            return Result.Failure<DriveSpaceResponse>(DomainErrors.Drive.SpaceNotFound);

        if (space.OwnerUserId != _userContext.UserId && !_access.IsAdmin)
        {
            var departmentIds = await _access.GetCallerDepartmentIdsAsync(cancellationToken);
            var reachable = await _spaces.ListSharedForUserAsync(_userContext.UserId, departmentIds, cancellationToken);
            if (reachable.All(candidate => candidate.SpaceId != space.SpaceId))
                return Result.Failure<DriveSpaceResponse>(DomainErrors.Drive.AccessDenied);
        }

        var setting = await _settings.GetSingletonAsync(cancellationToken);
        return Result.Success(DriveSpaceResponse.From(
            space, CreateDriveSpaceCommandHandler.EffectiveQuota(space, setting)));
    }
}

internal sealed class ListDriveSpacesQueryHandler : IQueryHandler<ListDriveSpacesQuery, DriveSpacesOverviewResponse>
{
    private readonly IDriveSpaceRepository _spaces;
    private readonly IInstitutionSettingRepository _settings;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;

    public ListDriveSpacesQueryHandler(
        IDriveSpaceRepository spaces,
        IInstitutionSettingRepository settings,
        IDriveAccessEvaluator access,
        IUserContext userContext)
    {
        _spaces = spaces;
        _settings = settings;
        _access = access;
        _userContext = userContext;
    }

    public async Task<Result<DriveSpacesOverviewResponse>> Handle(ListDriveSpacesQuery request, CancellationToken cancellationToken)
    {
        var setting = await _settings.GetSingletonAsync(cancellationToken);
        var personal = await _spaces.GetPersonalByOwnerAsync(_userContext.UserId, cancellationToken);
        var departmentIds = await _access.GetCallerDepartmentIdsAsync(cancellationToken);
        var shared = await _spaces.ListSharedForUserAsync(_userContext.UserId, departmentIds, cancellationToken);

        var sharedResponses = shared
            .Where(space => !space.IsPersonal)
            .Select(space => DriveSpaceResponse.From(space, CreateDriveSpaceCommandHandler.EffectiveQuota(space, setting)))
            .ToList();

        var personalResponse = personal is null
            ? null
            : DriveSpaceResponse.From(personal, CreateDriveSpaceCommandHandler.EffectiveQuota(personal, setting));

        return Result.Success(new DriveSpacesOverviewResponse(personalResponse, sharedResponses));
    }
}