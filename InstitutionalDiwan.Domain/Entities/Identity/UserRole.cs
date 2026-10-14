using System;
using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Domain.Entities.Identity;

public sealed class UserRole
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAt { get; private set; }
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
