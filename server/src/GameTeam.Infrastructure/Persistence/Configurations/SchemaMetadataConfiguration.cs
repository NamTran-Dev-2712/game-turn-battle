using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping cho <see cref="SchemaMetadata"/> — bảng <c>schema_metadata</c>, cột <c>snake_case</c> tường minh
/// (không dựa convention mơ hồ). Mapping tách theo <see cref="IEntityTypeConfiguration{TEntity}"/> (một file/entity).
/// </summary>
public sealed class SchemaMetadataConfiguration : IEntityTypeConfiguration<SchemaMetadata>
{
    public void Configure(EntityTypeBuilder<SchemaMetadata> builder)
    {
        builder.ToTable("schema_metadata");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Version)
            .HasColumnName("version")
            .IsRequired();

        // Seed neo schema version = 1 (ADR-007). Dùng HasData ⇒ seed nằm trong model snapshot, migration sinh
        // InsertData, không gây drift ('has-pending-model-changes' sạch).
        builder.HasData(new { Id = SchemaMetadata.SingletonId, Version = 1 });
    }
}
