using GameTeam.Domain.Gacha;
using GameTeam.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping cho <see cref="SummonRecord"/> — bảng <c>summon_records</c>, cột <c>snake_case</c> tường minh.
/// Id là <c>uuid</c> sinh ở code (<c>ValueGeneratedNever</c>). (<c>profile_id</c>, <c>request_id</c>) có ràng
/// buộc <b>unique</b> — bảo đảm idempotency tầng DB (ADR-007): một request = một bản ghi. Kết quả từng lần quay
/// lưu dạng cột JSON (<c>pulls</c>, EF9 <c>OwnsMany().ToJson()</c>). Domain event KHÔNG map (Ignore).
/// </summary>
public sealed class SummonRecordConfiguration : IEntityTypeConfiguration<SummonRecord>
{
    public void Configure(EntityTypeBuilder<SummonRecord> builder)
    {
        builder.ToTable("summon_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(x => x.RequestId).HasColumnName("request_id").IsRequired();
        builder.Property(x => x.BannerId).HasColumnName("banner_id").IsRequired();
        builder.Property(x => x.Count).HasColumnName("count").IsRequired();
        builder.Property(x => x.Seed).HasColumnName("seed").IsRequired();
        builder.Property(x => x.PityAfter).HasColumnName("pity_after").IsRequired();
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        // Idempotency tầng DB (ADR-007): một (profile, requestId) = một bản ghi (chống double khi race).
        builder.HasIndex(x => new { x.ProfileId, x.RequestId })
            .IsUnique()
            .HasDatabaseName("ix_summon_records_profile_request");

        builder.HasOne<PlayerProfile>()
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Kết quả từng lần quay: cột JSON (đọc lại nguyên vẹn cho retry idempotent).
        builder.OwnsMany(x => x.Pulls, pulls =>
        {
            pulls.ToJson("pulls");
            pulls.Property(p => p.HeroId).HasJsonPropertyName("hero_id");
            pulls.Property(p => p.Rarity).HasJsonPropertyName("rarity");
            pulls.Property(p => p.IsNew).HasJsonPropertyName("is_new");
            pulls.Property(p => p.Fragments).HasJsonPropertyName("fragments");
        });

        builder.Navigation(x => x.Pulls)
            .HasField("_pulls")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.DomainEvents);
    }
}
