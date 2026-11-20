using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>Creates a shared space owned by the caller. A quota override needs DRIVE_MANAGE.</summary>
public sealed record CreateDriveSpaceCommand(string Name, long? QuotaBytes = null) : ICommand<DriveSpaceResponse>;

/// <summary>Renames a space the caller owns (or manages as ADMIN).</summary>
public sealed record RenameDriveSpaceCommand(Guid SpaceId, string Name) : ICommand<DriveSpaceResponse>;

/// <summary>Sets a space's quota override (null restores the institution default). DRIVE_MANAGE only.</summary>
public sealed record SetDriveSpaceQuotaCommand(Guid SpaceId, long? QuotaBytes) : ICommand<DriveSpaceResponse>;

/// <summary>Lazily provisions the caller's personal space; idempotent.</summary>
public sealed record EnsurePersonalDriveSpaceCommand : ICommand<DriveSpaceResponse>;

internal sealed class CreateDriveSpaceCommandHandler : ICommandHandler<CreateDriveSpaceCommand, DriveSpaceResponse>
{
    private readonly IDriveSpaceRepository _spaces;
    private readonly IInstitutionSettingRepository _settings;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDriveSpaceCommandHandler(
        IDriveSpaceRepository spaces,
        IInstitutionSettingRepository settings,
        IDriveAccessEvaluator access,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _spaces = spaces;
        _settings = settings;
        _access = access;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveSpaceResponse>> Handle(CreateDriveSpaceCommand request, CancellationToken cancellationToken)
    {
        if (request.QuotaBytes is not null && !_access.IsAdmin)
            return Result.Failure<DriveSpaceResponse>(DomainErrors.Drive.AccessDenied);

        var created = DriveSpace.Create(
            request.Name, DriveSpaceType.SharedId, _userContext.UserId, request.QuotaBytes, _unitOfWork.UtcNow);
        if (created.IsFailure)
            return Result.Failure<DriveSpaceResponse>(created.Error);

        _spaces.Add(created.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var setting = await _settings.GetSingletonAsync(cancellationToken);
        return Result.Success(DriveSpaceResponse.From(created.Value, EffectiveQuota(created.Value, setting)));
    }

    internal static long EffectiveQuota(DriveSpace space, InstitutionSetting? setting) =>
        space.QuotaBytes ?? setting?.DriveDefaultQuotaBytes ?? InstitutionSetting.DefaultDriveQuotaBytes;
}

internal sealed class RenameDriveSpaceCommandHandler : ICommandHandler<RenameDriveSpaceCommand, DriveSpaceResponse>
{
    private readonly IDriveSpaceRepository _spaces;
    private readonly IInstitutionSettingRepository _settings;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public RenameDriveSpaceCommandHandler(
        IDriveSpaceRepository spaces,
        IInstitutionSettingRepository settings,
        IDriveAccessEvaluator access,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _spaces = spaces;
        _settings = settings;
        _access = access;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveSpaceResponse>> Handle(RenameDriveSpaceCommand request, CancellationToken cancellationToken)
    {
        var space = await _spaces.GetByIdAsync(request.SpaceId, cancellationToken);
        if (space is null)
            return Result.Failure<DriveSpaceResponse>(DomainErrors.Drive.SpaceNotFound);
        if (space.OwnerUserId != _userContext.UserId && !_access.IsAdmin)
            return Result.Failure<DriveSpaceResponse>(DomainErrors.Drive.AccessDenied);

        var renamed = space.Rename(request.Name, _unitOfWork.UtcNow);
        if (renamed.IsFailure)
            return Result.Failure<DriveSpaceResponse>(renamed.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var setting = await _settings.GetSingletonAsync(cancellationToken);
        return Result.Success(DriveSpaceResponse.From(space, CreateDriveSpaceCommandHandler.EffectiveQuota(space, setting)));
    }
}

internal sealed class SetDriveSpaceQuotaCommandHandler : ICommandHandler<SetDriveSpaceQuotaCommand, DriveSpaceResponse>
{
    private readonly IDriveSpaceRepository _spaces;
    private readonly IInstitutionSettingRepository _settings;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUnitOfWork _unitOfWork;

    public SetDriveSpaceQuotaCommandHandler(
        IDriveSpaceRepository spaces,
        IInstitutionSettingRepository settings,
        IDriveAccessEvaluator access,
        IUnitOfWork unitOfWork)
    {
        _spaces = spaces;
        _settings = settings;
        _access = access;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveSpaceResponse>> Handle(SetDriveSpaceQuotaCommand request, CancellationToken cancellationToken)
    {
        if (!_access.IsAdmin)
            return Result.Failure<DriveSpaceResponse>(DomainErrors.Drive.AccessDenied);

        var space = await _spaces.GetByIdAsync(request.SpaceId, cancellationToken);
        if (space is null)
            return Result.Failure<DriveSpaceResponse>(DomainErrors.Drive.SpaceNotFound);

        var set = space.SetQuota(request.QuotaBytes, _unitOfWork.UtcNow);
        if (set.IsFailure)
            return Result.Failure<DriveSpaceResponse>(set.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var setting = await _settings.GetSingletonAsync(cancellationToken);
        return Result.Success(DriveSpaceResponse.From(space, CreateDriveSpaceCommandHandler.EffectiveQuota(space, setting)));
    }
}

internal sealed class EnsurePersonalDriveSpaceCommandHandler : ICommandHandler<EnsurePersonalDriveSpaceCommand, DriveSpaceResponse>
{
    private readonly IDriveSpaceRepository _spaces;
    private readonly IInstitutionSettingRepository _settings;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public EnsurePersonalDriveSpaceCommandHandler(
        IDriveSpaceRepository spaces,
        IInstitutionSettingRepository settings,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _spaces = spaces;
        _settings = settings;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveSpaceResponse>> Handle(EnsurePersonalDriveSpaceCommand request, CancellationToken cancellationToken)
    {
        var setting = await _settings.GetSingletonAsync(cancellationToken);
        var existing = await _spaces.GetPersonalByOwnerAsync(_userContext.UserId, cancellationToken);
        if (existing is not null)
            return Result.Success(DriveSpaceResponse.From(existing, CreateDriveSpaceCommandHandler.EffectiveQuota(existing, setting)));

        var created = DriveSpace.CreatePersonal(_userContext.UserId, null, _unitOfWork.UtcNow);
        if (created.IsFailure)
            return Result.Failure<DriveSpaceResponse>(created.Error);

        _spaces.Add(created.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DriveSpaceResponse.From(created.Value, CreateDriveSpaceCommandHandler.EffectiveQuota(created.Value, setting)));
    }
}