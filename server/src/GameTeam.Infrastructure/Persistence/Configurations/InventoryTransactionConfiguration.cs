using GameTeam.Domain.Inventory;
using GameTeam.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping cho <see cref="InventoryTransaction"/> — bảng <c>inventory_transactions</c> (append-only), cột
/// <c>snake_case</c> tường minh. Id là <c>uuid</c> sinh ở code. <c>idempotency_key</c> có ràng buộc
/// <b>unique</b> — bảo đảm idempotency tầng DB (ADR-007): một key = một giao dịch (chống double-add/remove khi
/// race). Có index theo <c>profile_id</c> để truy vấn/audit. Các dòng thay đổi (<see cref="InventoryChange"/>)
/// lưu cột JSON (<c>changes</c>) — hỗ trợ nhiều-item/giao dịch. FK tới <c>player_profiles</c> (cascade).
/// </summary>
public sealed class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("inventory_transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(x => x.Direction).HasColumnName("direction").IsRequired();
        builder.Property(x => x.Source).HasColumnName("source").IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").IsRequired();
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        // Idempotency tầng DB (ADR-007): một idempotency_key = một giao dịch (backstop chống race double).
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ix_inventory_transactions_idempotency_key");

        // Truy vấn/audit theo profile.
        builder.HasIndex(x => x.ProfileId)
            .HasDatabaseName("ix_inventory_transactions_profile_id");

        builder.HasOne<PlayerProfile>()
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(x => x.Changes, changes =>
        {
            changes.ToJson("changes");
            changes.Property(c => c.ItemType).HasJsonPropertyName("item_type");
            changes.Property(c => c.ItemId).HasJsonPropertyName("item_id");
            changes.Property(c => c.Delta).HasJsonPropertyName("delta");
            changes.Property(c => c.QuantityAfter).HasJsonPropertyName("quantity_after");
        });

        builder.Navigation(x => x.Changes)
            .HasField("_changes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.DomainEvents);
    }
}
