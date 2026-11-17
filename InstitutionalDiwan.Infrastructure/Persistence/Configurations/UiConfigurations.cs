using InstitutionalDiwan.Domain.Entities.Ui;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstitutionalDiwan.Infrastructure.Persistence.Configurations;

public sealed class UiLabelOverrideConfiguration : IEntityTypeConfiguration<UiLabelOverride>
{
    public void Configure(EntityTypeBuilder<UiLabelOverride> builder)
    {
        builder.ToTable("UI_LABEL_OVERRIDES");
        builder.Ignore(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.LabelId);
        builder.Property(x => x.LabelId).HasColumnName("LabelId").ValueGeneratedNever();
        builder.Property(x => x.Key).HasColumnName("Key").HasMaxLength(UiLabelOverride.MaxKeyLength).IsRequired();
        builder.Property(x => x.Value).HasColumnName("Value").HasMaxLength(UiLabelOverride.MaxValueLength).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("UpdatedByUserId");

        builder.HasIndex(x => x.Key).IsUnique().HasDatabaseName("UX_UI_LABEL_OVERRIDES_KEY");
    }
}