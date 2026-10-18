using System;
using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Domain.Entities.Identity;

public sealed class UserRole
{
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid UserId { get; private set; }
    // FK → Role.RoleId (InstitutionalDiwan.Domain.Entities.Identity.Role)
    public Guid RoleId { get; private set; }
    public DateTime AssignedAt { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid? AssignedByUserId { get; private set; }

    private UserRole()
    {
    }

    internal static Result<UserRole> Create(Guid userId, Guid roleId, Guid? assignedByUserId = null)
    {
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = assignedByUserId
        };

        return Result.Success(userRole);
    }
}
