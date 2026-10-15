using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
        builder.Property(x => x.DepartmentTypeCode).HasColumnName("DepartmentTypeCode").HasMaxLength(50).IsRequired();
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
        builder.HasIndex(x => new { x.DepartmentTypeCode, x.IsActive }).HasDatabaseName("IX_DEPARTMENTS_TYPE_ACTIVE");

        builder.HasOne<DepartmentType>()
            .WithMany()
            .HasForeignKey(x => x.DepartmentTypeCode)
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
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasColumnName("Code").HasMaxLength(50).ValueGeneratedNever();
        builder.Property(x => x.NameAr).HasColumnName("NameAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("NameEn").HasMaxLength(150);
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
        builder.Property(x => x.NumberingMode).HasColumnName("NumberingMode").HasMaxLength(50).IsRequired();
        builder.Property(x => x.NumberingResetPolicy).HasColumnName("NumberingResetPolicy").HasMaxLength(50).IsRequired();
        builder.Property(x => x.DefaultTimezone).HasColumnName("DefaultTimezone").HasMaxLength(100).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}
