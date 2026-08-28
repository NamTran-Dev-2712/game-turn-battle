using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping for <see cref="ConfigBundleRecord"/> — table <c>config_bundles</c>, explicit
/// <c>snake_case</c> columns (Phase 21). One immutable row per <c>config@vN</c>; the version is the PK
/// (<see cref="ConfigBundleRecord.Version"/> maps to <c>id</c>, code-assigned ⇒ <c>ValueGeneratedNever</c>).
/// </summary>
public sealed class ConfigBundleRecordConfiguration : IEntityTypeConfiguration<ConfigBundleRecord>
{
    public void Configure(EntityTypeBuilder<ConfigBundleRecord> builder)
    {
        builder.ToTable("config_bundles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("version")
            .ValueGeneratedNever();

        builder.Property(x => x.ConfigVersion)
            .HasColumnName("config_version")
            .IsRequired();

        builder.Property(x => x.SchemaVersion)
            .HasColumnName("schema_version")
            .IsRequired();

        builder.Property(x => x.Checksum)
            .HasColumnName("checksum")
            .IsRequired();

        builder.Property(x => x.GeneratedAt)
            .HasColumnName("generated_at")
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .IsRequired();

        // Unique version label (defensive — the PK already guarantees one row per version).
        builder.HasIndex(x => x.ConfigVersion)
            .IsUnique()
            .HasDatabaseName("ix_config_bundles_config_version");
    }
}
