using GameTeam.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainInventory = GameTeam.Domain.Inventory.Inventory;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping cho <see cref="DomainInventory"/> — bảng <c>inventories</c>, cột <c>snake_case</c> tường minh. Id là
/// <c>uuid</c> sinh ở code. <c>profile_id</c> có FK tới <c>player_profiles.id</c> (cascade) + ràng buộc
/// <b>unique</b> (một kho / một profile). Các chồng tài sản (<c>ItemStack</c>) lưu cột JSON (<c>stacks</c>, EF9
/// <c>OwnsMany().ToJson()</c>).
/// </summary>
public sealed class InventoryConfiguration : IEntityTypeConfiguration<DomainInventory>
{
    public void Configure(EntityTypeBuilder<DomainInventory> builder)
    {
        builder.ToTable("inventories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(x => x.ProfileId)
            .IsUnique()
            .HasDatabaseName("ix_inventories_profile_id");

        builder.HasOne<PlayerProfile>()
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(x => x.Stacks, stacks =>
        {
            stacks.ToJson("stacks");
            stacks.Property(s => s.ItemType).HasJsonPropertyName("item_type");
            stacks.Property(s => s.ItemId).HasJsonPropertyName("item_id");
            stacks.Property(s => s.Quantity).HasJsonPropertyName("quantity");
        });

        builder.Navigation(x => x.Stacks)
            .HasField("_stacks")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.DomainEvents);
    }
}
