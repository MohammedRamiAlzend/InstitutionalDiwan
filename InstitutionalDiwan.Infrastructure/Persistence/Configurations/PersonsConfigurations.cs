using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Persons;
using InstitutionalDiwan.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("PERSONS");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.PersonId);
        builder.Property(x => x.PersonId).HasColumnName("PersonId").ValueGeneratedNever();
        builder.Property(x => x.FullName).HasColumnName("FullName").HasMaxLength(Person.MaxFullNameLength);
        builder.Property(x => x.DisplayName).HasColumnName("DisplayName").HasMaxLength(Person.MaxDisplayNameLength).IsRequired();
        builder.Property(x => x.NormalizedName).HasColumnName("NormalizedName").HasMaxLength(Person.MaxDisplayNameLength).IsRequired();
        builder.Property(x => x.FirstName).HasColumnName("FirstName").HasMaxLength(Person.MaxNamePartLength);
        builder.Property(x => x.LastName).HasColumnName("LastName").HasMaxLength(Person.MaxNamePartLength);
        builder.Property(x => x.FatherName).HasColumnName("FatherName").HasMaxLength(Person.MaxNamePartLength);
        builder.Property(x => x.MotherName).HasColumnName("MotherName").HasMaxLength(Person.MaxNamePartLength);
        builder.Property(x => x.NationalNumber).HasColumnName("NationalNumber").HasMaxLength(Person.MaxNationalNumberLength);
        builder.Property(x => x.CivilRegistryNumber).HasColumnName("CivilRegistryNumber").HasMaxLength(Person.MaxCivilRegistryNumberLength);
        builder.Property(x => x.CivilRegistryPlace).HasColumnName("CivilRegistryPlace").HasMaxLength(Person.MaxPlaceLength);
        builder.Property(x => x.BirthPlace).HasColumnName("BirthPlace").HasMaxLength(Person.MaxPlaceLength);
        builder.Property(x => x.BirthDate).HasColumnName("BirthDate").HasColumnType("date");
        builder.Property(x => x.GenderId).HasColumnName("GenderId");
        builder.Property(x => x.NationalityId).HasColumnName("NationalityId");
        builder.Property(x => x.Phone).HasColumnName("Phone").HasMaxLength(Person.MaxPhoneLength);
        builder.Property(x => x.Address).HasColumnName("Address").HasMaxLength(Person.MaxAddressLength);
        builder.Property(x => x.Notes).HasColumnName("Notes").HasMaxLength(Person.MaxNotesLength);
        builder.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");
        builder.Property(x => x.RowVersion).HasColumnName("RowVersion").IsRowVersion();

        builder.HasIndex(x => x.NormalizedName).HasDatabaseName("IX_PERSONS_NORMALIZED_NAME");
        builder.HasIndex(x => x.NationalNumber)
            .IsUnique()
            .HasFilter("[NationalNumber] IS NOT NULL")
            .HasDatabaseName("UX_PERSONS_NATIONAL_NUMBER");

        builder.HasOne<Gender>().WithMany().HasForeignKey(x => x.GenderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Nationality>().WithMany().HasForeignKey(x => x.NationalityId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DocumentPersonConfiguration : IEntityTypeConfiguration<DocumentPerson>
{
    public void Configure(EntityTypeBuilder<DocumentPerson> builder)
    {
        builder.ToTable("DOCUMENT_PERSONS");
        builder.Ignore(x => x.Id);

        builder.HasKey(x => x.DocumentPersonId);
        builder.Property(x => x.DocumentPersonId).HasColumnName("DocumentPersonId").ValueGeneratedNever();
        builder.Property(x => x.DocumentId).HasColumnName("DocumentId").IsRequired();
        builder.Property(x => x.PersonId).HasColumnName("PersonId").IsRequired();
        builder.Property(x => x.RoleId).HasColumnName("RoleId").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();

        builder.HasIndex(x => new { x.DocumentId, x.PersonId }).IsUnique().HasDatabaseName("UX_DOCUMENT_PERSONS_DOC_PERSON");
        builder.HasIndex(x => new { x.PersonId, x.DocumentId }).HasDatabaseName("IX_DOCUMENT_PERSONS_PERSON_DOC");

        builder.HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentPersonRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DocumentPersonRoleConfiguration : IEntityTypeConfiguration<DocumentPersonRole>
{
    public void Configure(EntityTypeBuilder<DocumentPersonRole> builder) => PersonLookupMapping.Map(builder, "DOCUMENT_PERSON_ROLES");
}

public sealed class GenderConfiguration : IEntityTypeConfiguration<Gender>
{
    public void Configure(EntityTypeBuilder<Gender> builder) => PersonLookupMapping.Map(builder, "GENDERS");
}

public sealed class NationalityConfiguration : IEntityTypeConfiguration<Nationality>
{
    public void Configure(EntityTypeBuilder<Nationality> builder) => PersonLookupMapping.Map(builder, "NATIONALITIES");
}

internal static class PersonLookupMapping
{
    public static void Map<TEntity>(EntityTypeBuilder<TEntity> builder, string table)
        where TEntity : LookupEntity
    {
        builder.ToTable(table);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(x => x.Desc).HasColumnName("Desc").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescAr).HasColumnName("DescAr").HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(3)").IsRequired();
    }
}
