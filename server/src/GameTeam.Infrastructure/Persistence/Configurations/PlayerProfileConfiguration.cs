using GameTeam.Domain.Accounts;
using GameTeam.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping cho <see cref="PlayerProfile"/> — bảng <c>player_profiles</c>, cột <c>snake_case</c> tường minh.
/// Id là <c>uuid</c> sinh ở code (<c>ValueGeneratedNever</c>). <c>account_id</c> có <b>unique index</b> +
/// khoá ngoại tới <c>accounts.id</c> ⇒ quan hệ 1-1, và là bảo đảm <b>idempotency ở tầng DB</b> (không thể tạo
/// hai profile cho một account — ADR-007). Domain event KHÔNG map (Ignore).
/// </summary>
public sealed class PlayerProfileConfiguration : IEntityTypeConfiguration<PlayerProfile>
{
    public void Configure(EntityTypeBuilder<PlayerProfile> builder)
    {
        builder.ToTable("player_profiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .IsRequired();

        builder.Property(x => x.Level)
            .HasColumnName("level")
            .IsRequired();

        builder.Property(x => x.SchemaVersion)
            .HasColumnName("schema_version")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Idempotency ở tầng DB: mỗi account chỉ một profile (unique) + FK tới accounts.
        builder.HasIndex(x => x.AccountId)
            .IsUnique()
            .HasDatabaseName("ix_player_profiles_account_id");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Domain event chỉ để dispatch (Phase 11) — không phải cột.
        builder.Ignore(x => x.DomainEvents);
    }
}
