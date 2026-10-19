using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("DEPARTMENTS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.DepartmentId);
        builder.Property(x => x.DepartmentId).HasColumnName("DepartmentId").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("Code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.NameAr).HasColumnName("NameAr").HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("NameEn").HasMaxLength(200);
        builder.Property(x => x.DepartmentTypeId).HasColumnName("DepartmentTypeId").IsRequired();
        builder.Property(x => x.ParentDepartmentId).HasColumnName("ParentDepartmentId");
        builder.Property(x => x.ManagerEmployeeId).HasColumnName("ManagerEmployeeId");
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CanSendExternal).HasColumnName("CanSendExternal").IsRequired();
        builder.Property(x => x.CanReceiveExternal).HasColumnName("CanReceiveExternal").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("Notes").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_DEPARTMENTS_CODE");
        builder.HasIndex(x => x.ParentDepartmentId).HasDatabaseName("IX_DEPARTMENTS_PARENT");
        builder.HasIndex(x => new { x.DepartmentTypeId, x.IsActive }).HasDatabaseName("IX_DEPARTMENTS_TYPE_ACTIVE");

        builder.HasOne<DepartmentType>()
            .WithMany()
            .HasForeignKey(x => x.DepartmentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(x => x.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DepartmentTypeConfiguration : IEntityTypeConfiguration<DepartmentType>
{
    public void Configure(EntityTypeBuilder<DepartmentType> builder)
    {
        builder.ToTable("DEPARTMENT_TYPES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.HierarchyLevelOrder).HasColumnName("HierarchyLevelOrder").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}

public sealed class InstitutionSettingConfiguration : IEntityTypeConfiguration<InstitutionSetting>
{
    public void Configure(EntityTypeBuilder<InstitutionSetting> builder)
    {
        builder.ToTable("INSTITUTION_SETTINGS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.SettingId);
        builder.Property(x => x.SettingId).HasColumnName("SettingId").ValueGeneratedNever();
        builder.Property(x => x.InstitutionNameAr).HasColumnName("InstitutionNameAr").HasMaxLength(250).IsRequired();
        builder.Property(x => x.InstitutionNameEn).HasColumnName("InstitutionNameEn").HasMaxLength(250);
        builder.Property(x => x.LogoPath).HasColumnName("LogoPath").HasMaxLength(500);
        builder.Property(x => x.NumberingModeId).HasColumnName("NumberingModeId").IsRequired();
        builder.Property(x => x.NumberingResetPolicyId).HasColumnName("NumberingResetPolicyId").IsRequired();
        builder.Property(x => x.DefaultTimezone).HasColumnName("DefaultTimezone").HasMaxLength(100).IsRequired();
        builder.Property(x => x.CentralDiwanDepartmentId).HasColumnName("CentralDiwanDepartmentId");
        builder.Property(x => x.ResponsibleEmployeeId).HasColumnName("ResponsibleEmployeeId");
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => x.CentralDiwanDepartmentId).HasDatabaseName("IX_INSTITUTION_SETTINGS_CENTRAL_DIWAN");

        builder.HasOne<NumberingMode>()
            .WithMany()
            .HasForeignKey(x => x.NumberingModeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<NumberingResetPolicy>()
            .WithMany()
            .HasForeignKey(x => x.NumberingResetPolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(x => x.CentralDiwanDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.ResponsibleEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NumberingModeConfiguration : IEntityTypeConfiguration<NumberingMode>
{
    public void Configure(EntityTypeBuilder<NumberingMode> builder)
    {
        builder.ToTable("NUMBERING_MODES");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}
