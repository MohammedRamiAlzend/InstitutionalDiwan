using FluentValidation;
using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>Grants (or updates) a user/department role on an item. Manage only; notifies the grantee.</summary>
public sealed record GrantDrivePermissionCommand(Guid ItemId, int PrincipalTypeId, Guid PrincipalId, int RoleId)
    : ICommand<DrivePermissionResponse>;

/// <summary>Revokes an explicit grant. Manage only.</summary>
public sealed record RevokeDrivePermissionCommand(Guid PermissionId) : ICommand;

/// <summary>Breaks or restores permission inheritance on an item. Manage only.</summary>
public sealed record SetDriveItemInheritanceCommand(Guid ItemId, bool BreakInheritance) : ICommand<DriveItemResponse>;

internal sealed class GrantDrivePermissionCommandHandler : ICommandHandler<GrantDrivePermissionCommand, DrivePermissionResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDrivePermissionRepository _permissions;
    private readonly IDriveAccessEvaluator _access;
    private readonly INotificationRecipientDirectory _directory;
    private readonly IDriveNotificationPublisher _notifications;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public GrantDrivePermissionCommandHandler(
        IDriveItemRepository items,
        IDrivePermissionRepository permissions,
        IDriveAccessEvaluator access,
        INotificationRecipientDirectory directory,
        IDriveNotificationPublisher notifications,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _permissions = permissions;
        _access = access;
        _directory = directory;
        _notifications = notifications;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DrivePermissionResponse>> Handle(GrantDrivePermissionCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure<DrivePermissionResponse>(DomainErrors.Drive.ItemNotFound);
        if (!await _access.CanAccessItemAsync(request.ItemId, DriveRole.ManagerId, cancellationToken))
            return Result.Failure<DrivePermissionResponse>(DomainErrors.Drive.AccessDenied);

        var now = _unitOfWork.UtcNow;
        var existing = await _permissions.FindAsync(request.ItemId, request.PrincipalTypeId, request.PrincipalId, cancellationToken);
        DrivePermission permission;
        if (existing is not null)
        {
            var changed = existing.ChangeRole(request.RoleId);
            if (changed.IsFailure)
                return Result.Failure<DrivePermissionResponse>(changed.Error);
            permission = existing;
        }
        else
        {
            var created = DrivePermission.Create(
                request.ItemId, request.PrincipalTypeId, request.PrincipalId, request.RoleId, _userContext.UserId, now);
            if (created.IsFailure)
                return Result.Failure<DrivePermissionResponse>(created.Error);
            permission = created.Value;
            _permissions.Add(permission);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var recipients = await ResolveRecipientsAsync(request.PrincipalTypeId, request.PrincipalId, now, cancellationToken);
        await _notifications.PublishAsync(
            NotificationType.DrivePermissionGrantedId, recipients, item.Name, _userContext.UserId,
            _userContext.Username, $"/drive/items/{item.ItemId}", cancellationToken);

        return Result.Success(DrivePermissionResponse.From(permission));
    }

    private async Task<IReadOnlyCollection<Guid>> ResolveRecipientsAsync(
        int principalTypeId, Guid principalId, DateTime now, CancellationToken cancellationToken)
    {
        if (principalTypeId == DrivePrincipalType.UserId)
            return new[] { principalId };

        var users = await _directory.GetDepartmentUsersAsync(principalId, now, cancellationToken);
        return users.Select(user => user.UserId).ToList();
    }
}

internal sealed class RevokeDrivePermissionCommandHandler : ICommandHandler<RevokeDrivePermissionCommand>
{
    private readonly IDrivePermissionRepository _permissions;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeDrivePermissionCommandHandler(
        IDrivePermissionRepository permissions,
        IDriveAccessEvaluator access,
        IUnitOfWork unitOfWork)
    {
        _permissions = permissions;
        _access = access;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RevokeDrivePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _permissions.GetByIdAsync(request.PermissionId, cancellationToken);
        if (permission is null)
            return Result.Failure(DomainErrors.Drive.PermissionNotFound);
        if (!await _access.CanAccessItemAsync(permission.ItemId, DriveRole.ManagerId, cancellationToken))
            return Result.Failure(DomainErrors.Drive.AccessDenied);

        _permissions.Remove(permission);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class SetDriveItemInheritanceCommandHandler : ICommandHandler<SetDriveItemInheritanceCommand, DriveItemResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUnitOfWork _unitOfWork;

    public SetDriveItemInheritanceCommandHandler(
        IDriveItemRepository items,
        IDriveAccessEvaluator access,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _access = access;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveItemResponse>> Handle(SetDriveItemInheritanceCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.ItemNotFound);
        if (!await _access.CanAccessItemAsync(request.ItemId, DriveRole.ManagerId, cancellationToken))
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.AccessDenied);

        var result = request.BreakInheritance
            ? item.BreakPermissionInheritance(_unitOfWork.UtcNow)
            : item.RestorePermissionInheritance(_unitOfWork.UtcNow);
        if (result.IsFailure)
            return Result.Failure<DriveItemResponse>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DriveItemResponse.From(item));
    }
}

public sealed class GrantDrivePermissionCommandValidator : AbstractValidator<GrantDrivePermissionCommand>
{
    public GrantDrivePermissionCommandValidator()
    {
        RuleFor(command => command.ItemId).NotEmpty().WithErrorCode("Drive.ItemNotFound");
        RuleFor(command => command.PrincipalId).NotEmpty().WithErrorCode("Drive.PrincipalIdEmpty");
        RuleFor(command => command.PrincipalTypeId)
            .Must(DrivePrincipalType.IsValid).WithErrorCode("Drive.PrincipalTypeInvalid");
        RuleFor(command => command.RoleId)
            .Must(DriveRole.IsValid).WithErrorCode("Drive.RoleInvalid");
    }
}

public sealed class RevokeDrivePermissionCommandValidator : AbstractValidator<RevokeDrivePermissionCommand>
{
    public RevokeDrivePermissionCommandValidator()
    {
        RuleFor(command => command.PermissionId).NotEmpty().WithErrorCode("Drive.PermissionNotFound");
    }
}

public sealed class SetDriveItemInheritanceCommandValidator : AbstractValidator<SetDriveItemInheritanceCommand>
{
    public SetDriveItemInheritanceCommandValidator()
    {
        RuleFor(command => command.ItemId).NotEmpty().WithErrorCode("Drive.ItemNotFound");
    }
}