using InstitutionalDiwan.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("USERS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).HasColumnName("UserId").ValueGeneratedNever();
        builder.Property(x => x.Username).HasColumnName("Username").HasMaxLength(100).IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("PasswordHash").HasMaxLength(500).IsRequired();
        builder.Property(x => x.EmployeeId).HasColumnName("EmployeeId");
        builder.Property(x => x.PrimaryDepartmentId).HasColumnName("PrimaryDepartmentId").IsRequired();
        builder.Property(x => x.ScopeTypeId).HasColumnName("ScopeTypeId").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.MustChangePassword).HasColumnName("MustChangePassword").IsRequired();
        builder.Property(x => x.LastLoginAt).HasColumnName("LastLoginAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

        builder.HasIndex(x => x.Username).IsUnique().HasDatabaseName("UX_USERS_USERNAME");
        builder.HasIndex(x => x.PrimaryDepartmentId).HasDatabaseName("IX_USERS_PRIMARY_DEPT");

        builder.HasOne<UserScopeType>()
            .WithMany()
            .HasForeignKey(x => x.ScopeTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.UserRoles)
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.UserRoles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.DepartmentAssignments)
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.DepartmentAssignments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("ROLES");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.RoleId);
        builder.Property(x => x.RoleId).HasColumnName("RoleId").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("Code").HasMaxLength(100).IsRequired();
        builder.Property(x => x.NameAr).HasColumnName("NameAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("NameEn").HasMaxLength(150);
        builder.Property(x => x.Description).HasColumnName("Description").HasMaxLength(500);
        builder.Property(x => x.IsSystemRole).HasColumnName("IsSystemRole").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_ROLES_CODE");

        builder.HasMany(x => x.RolePermissions)
            .WithOne()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.RolePermissions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("PERMISSIONS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasColumnName("Code").HasMaxLength(120).ValueGeneratedNever();
        builder.Property(x => x.NameAr).HasColumnName("NameAr").HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("NameEn").HasMaxLength(200);
        builder.Property(x => x.ModuleName).HasColumnName("ModuleName").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasColumnName("Description").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("USER_ROLES");
        builder.HasKey(x => new { x.UserId, x.RoleId });
        builder.Property(x => x.UserId).HasColumnName("UserId");
        builder.Property(x => x.RoleId).HasColumnName("RoleId");
        builder.Property(x => x.AssignedAt).HasColumnName("AssignedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.AssignedByUserId).HasColumnName("AssignedByUserId");
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("ROLE_PERMISSIONS");
        builder.HasKey(x => new { x.RoleId, x.PermissionCode });
        builder.Property(x => x.RoleId).HasColumnName("RoleId");
        builder.Property(x => x.PermissionCode).HasColumnName("PermissionCode").HasMaxLength(120);
        builder.Property(x => x.GrantedAt).HasColumnName("GrantedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("EMPLOYEES");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.EmployeeId);
        builder.Property(x => x.EmployeeId).HasColumnName("EmployeeId").ValueGeneratedNever();
        builder.Property(x => x.EmployeeNumber).HasColumnName("EmployeeNumber").HasMaxLength(50);
        builder.Property(x => x.FullNameAr).HasColumnName("FullNameAr").HasMaxLength(200).IsRequired();
        builder.Property(x => x.FullNameEn).HasColumnName("FullNameEn").HasMaxLength(200);
        builder.Property(x => x.DepartmentId).HasColumnName("DepartmentId").IsRequired();
        builder.Property(x => x.JobTitleAr).HasColumnName("JobTitleAr").HasMaxLength(150);
        builder.Property(x => x.Email).HasColumnName("Email").HasMaxLength(150);
        builder.Property(x => x.Phone).HasColumnName("Phone").HasMaxLength(50);
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

        builder.HasIndex(x => x.DepartmentId).HasDatabaseName("IX_EMPLOYEES_DEPT");
    }
}

public sealed class UserDepartmentAssignmentConfiguration : IEntityTypeConfiguration<UserDepartmentAssignment>
{
    public void Configure(EntityTypeBuilder<UserDepartmentAssignment> builder)
    {
        builder.ToTable("USER_DEPARTMENT_ASSIGNMENTS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.AssignmentId);
        builder.Property(x => x.AssignmentId).HasColumnName("AssignmentId").ValueGeneratedNever();
        builder.Property(x => x.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(x => x.DepartmentId).HasColumnName("DepartmentId").IsRequired();
        builder.Property(x => x.AssignmentTypeId).HasColumnName("AssignmentTypeId").IsRequired();
        builder.Property(x => x.IncludeSubtree).HasColumnName("IncludeSubtree").IsRequired();
        builder.Property(x => x.StartDate).HasColumnName("StartDate").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.EndDate).HasColumnName("EndDate").HasColumnType("datetime2(3)");
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => new { x.UserId, x.IsActive }).HasDatabaseName("IX_USER_DEPT_ASSIGNMENTS_USER_ACTIVE");

        builder.HasOne<AssignmentType>()
            .WithMany()
            .HasForeignKey(x => x.AssignmentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DocumentAccessGrantConfiguration : IEntityTypeConfiguration<DocumentAccessGrant>
{
    public void Configure(EntityTypeBuilder<DocumentAccessGrant> builder)
    {
        builder.ToTable("DOCUMENT_ACCESS_GRANTS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.GrantId);
        builder.Property(x => x.GrantId).HasColumnName("GrantId").ValueGeneratedNever();
        builder.Property(x => x.DocumentId).HasColumnName("DocumentId").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(x => x.AccessLevelId).HasColumnName("AccessLevelId").IsRequired();
        builder.Property(x => x.GrantedByUserId).HasColumnName("GrantedByUserId").IsRequired();
        builder.Property(x => x.Reason).HasColumnName("Reason").HasMaxLength(500);
        builder.Property(x => x.GrantedAt).HasColumnName("GrantedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("ExpiresAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.IsRevoked).HasColumnName("IsRevoked").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnName("RevokedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.RevokedByUserId).HasColumnName("RevokedByUserId");

        builder.HasIndex(x => new { x.DocumentId, x.UserId, x.IsRevoked }).HasDatabaseName("IX_DOC_ACCESS_GRANTS_DOC_USER");

        builder.HasOne<AccessLevel>()
            .WithMany()
            .HasForeignKey(x => x.AccessLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserScopeTypeConfiguration : IEntityTypeConfiguration<UserScopeType>
{
    public void Configure(EntityTypeBuilder<UserScopeType> builder)
    {
        builder.ToTable("USER_SCOPE_TYPES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class AssignmentTypeConfiguration : IEntityTypeConfiguration<AssignmentType>
{
    public void Configure(EntityTypeBuilder<AssignmentType> builder)
    {
        builder.ToTable("ASSIGNMENT_TYPES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class AccessLevelConfiguration : IEntityTypeConfiguration<AccessLevel>
{
    public void Configure(EntityTypeBuilder<AccessLevel> builder)
    {
        builder.ToTable("ACCESS_LEVELS");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

