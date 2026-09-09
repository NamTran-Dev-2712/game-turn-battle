using GameTeam.Domain.Battles;
using GameTeam.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping cho <see cref="BattleRecord"/> — bảng <c>battle_records</c>, cột <c>snake_case</c> tường minh.
/// Id là <c>uuid</c> sinh ở code (<c>ValueGeneratedNever</c>). (<c>profile_id</c>, <c>attempt_id</c>) có ràng
/// buộc <b>unique</b> — bảo đảm idempotency tầng DB (ADR-007): một attempt = một bản ghi. Thưởng đã cấp lưu
/// dạng cột JSON (<c>rewards</c>, EF9 <c>OwnsMany().ToJson()</c>); event log lưu cột text (<c>log</c>).
/// Domain event KHÔNG map (Ignore).
/// </summary>
public sealed class BattleRecordConfiguration : IEntityTypeConfiguration<BattleRecord>
{
    public void Configure(EntityTypeBuilder<BattleRecord> builder)
    {
        builder.ToTable("battle_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(x => x.AttemptId).HasColumnName("attempt_id").IsRequired();
        builder.Property(x => x.TeamId).HasColumnName("team_id").IsRequired();
        builder.Property(x => x.StageId).HasColumnName("stage_id").IsRequired();
        builder.Property(x => x.Seed).HasColumnName("seed").IsRequired();
        builder.Property(x => x.Outcome).HasColumnName("outcome").IsRequired();
        builder.Property(x => x.Rounds).HasColumnName("rounds").IsRequired();
        builder.Property(x => x.Log).HasColumnName("log").IsRequired();
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        // Idempotency tầng DB (ADR-007): một (profile, attemptId) = một bản ghi (chống double-grant khi race).
        builder.HasIndex(x => new { x.ProfileId, x.AttemptId })
            .IsUnique()
            .HasDatabaseName("ix_battle_records_profile_attempt");

        builder.HasOne<PlayerProfile>()
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Thưởng đã cấp: cột JSON (đọc lại nguyên vẹn cho retry idempotent).
        builder.OwnsMany(x => x.Rewards, rewards =>
        {
            rewards.ToJson("rewards");
            rewards.Property(r => r.RewardType).HasJsonPropertyName("reward_type");
            rewards.Property(r => r.RefId).HasJsonPropertyName("ref_id");
            rewards.Property(r => r.Amount).HasJsonPropertyName("amount");
        });

        builder.Navigation(x => x.Rewards)
            .HasField("_rewards")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.DomainEvents);
    }
}
