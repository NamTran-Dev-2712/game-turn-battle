using GameTeam.Domain.Gacha;
using GameTeam.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping cho <see cref="GachaPity"/> — bảng <c>gacha_pity</c>, cột <c>snake_case</c> tường minh. Id là
/// <c>uuid</c> sinh ở code (<c>ValueGeneratedNever</c>). (<c>profile_id</c>, <c>banner_id</c>) có ràng buộc
/// <b>unique</b> — một bộ đếm pity cho mỗi (profile, banner). Domain event KHÔNG map (Ignore).
/// </summary>
public sealed class GachaPityConfiguration : IEntityTypeConfiguration<GachaPity>
{
    public void Configure(EntityTypeBuilder<GachaPity> builder)
    {
        builder.ToTable("gacha_pity");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(x => x.BannerId).HasColumnName("banner_id").IsRequired();
        builder.Property(x => x.Count).HasColumnName("count").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(x => new { x.ProfileId, x.BannerId })
            .IsUnique()
            .HasDatabaseName("ix_gacha_pity_profile_banner");

        builder.HasOne<PlayerProfile>()
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
