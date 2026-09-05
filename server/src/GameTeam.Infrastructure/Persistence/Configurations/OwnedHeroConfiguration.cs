using GameTeam.Domain.Heroes;
using GameTeam.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping cho <see cref="OwnedHero"/> — bảng <c>owned_heroes</c>, cột <c>snake_case</c> tường minh. Id là
/// <c>uuid</c> sinh ở code (<c>ValueGeneratedNever</c>). <c>profile_id</c> có khoá ngoại tới
/// <c>player_profiles.id</c> (cascade khi profile bị xoá) + index; ràng buộc <b>unique (profile_id, hero_id)</b>
/// chống cấp trùng một hero cho cùng profile (idempotency tầng DB). Domain event KHÔNG map (Ignore).
/// </summary>
public sealed class OwnedHeroConfiguration : IEntityTypeConfiguration<OwnedHero>
{
    public void Configure(EntityTypeBuilder<OwnedHero> builder)
    {
        builder.ToTable("owned_heroes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.Property(x => x.HeroId)
            .HasColumnName("hero_id")
            .IsRequired();

        builder.Property(x => x.Level)
            .HasColumnName("level")
            .IsRequired();

        builder.Property(x => x.Stars)
            .HasColumnName("stars")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => x.ProfileId)
            .HasDatabaseName("ix_owned_heroes_profile_id");

        // Idempotency tầng DB: một hero definition chỉ cấp một lần cho một profile (Phase 27 nền — summon
        // thật ở phase 33 sẽ quyết định chính sách trùng lặp/duplicate; ràng buộc này giữ seed sạch).
        builder.HasIndex(x => new { x.ProfileId, x.HeroId })
            .IsUnique()
            .HasDatabaseName("ix_owned_heroes_profile_id_hero_id");

        builder.HasOne<PlayerProfile>()
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
