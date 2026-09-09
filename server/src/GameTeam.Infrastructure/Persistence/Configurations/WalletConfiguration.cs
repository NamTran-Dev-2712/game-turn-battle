using GameTeam.Domain.Economy;
using GameTeam.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping cho <see cref="Wallet"/> — bảng <c>wallets</c>, cột <c>snake_case</c> tường minh. Id là <c>uuid</c>
/// sinh ở code. <c>profile_id</c> có FK tới <c>player_profiles.id</c> (cascade) + ràng buộc <b>unique</b> (một ví
/// / một profile). Số dư (<see cref="WalletBalance"/>) lưu cột JSON (<c>balances</c>, EF9 <c>OwnsMany().ToJson()</c>).
/// </summary>
public sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(x => x.ProfileId)
            .IsUnique()
            .HasDatabaseName("ix_wallets_profile_id");

        builder.HasOne<PlayerProfile>()
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(x => x.Balances, balances =>
        {
            balances.ToJson("balances");
            balances.Property(b => b.Currency).HasJsonPropertyName("currency");
            balances.Property(b => b.Amount).HasJsonPropertyName("amount");
        });

        builder.Navigation(x => x.Balances)
            .HasField("_balances")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.DomainEvents);
    }
}
