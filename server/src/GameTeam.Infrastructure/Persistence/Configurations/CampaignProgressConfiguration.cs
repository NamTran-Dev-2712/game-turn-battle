using GameTeam.Domain.Campaign;
using GameTeam.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping cho <see cref="CampaignProgress"/> — bảng <c>campaign_progress</c> + owned collection
/// <c>campaign_cleared_stages</c>, cột <c>snake_case</c> tường minh. Id là <c>uuid</c> sinh ở code
/// (<c>ValueGeneratedNever</c>). <c>profile_id</c> có khoá ngoại tới <c>player_profiles.id</c> (cascade) +
/// ràng buộc <b>unique</b> (một tiến độ / một profile — idempotency tầng DB). Stage đã clear
/// (<see cref="ClearedStage"/>) là owned entity, PK ghép <c>(campaign_progress_id, stage_id)</c> (chống clear
/// trùng ở DB). Domain event KHÔNG map (Ignore).
/// </summary>
public sealed class CampaignProgressConfiguration : IEntityTypeConfiguration<CampaignProgress>
{
    public void Configure(EntityTypeBuilder<CampaignProgress> builder)
    {
        builder.ToTable("campaign_progress");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.Property(x => x.CurrentAfkStageId)
            .HasColumnName("current_afk_stage_id")
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

        // Idempotency tầng DB: một tiến độ campaign cho một profile.
        builder.HasIndex(x => x.ProfileId)
            .IsUnique()
            .HasDatabaseName("ix_campaign_progress_profile_id");

        builder.HasOne<PlayerProfile>()
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(x => x.ClearedStages, cleared =>
        {
            cleared.ToTable("campaign_cleared_stages");

            cleared.WithOwner().HasForeignKey("campaign_progress_id");

            cleared.Property<Guid>("campaign_progress_id").HasColumnName("campaign_progress_id");

            cleared.Property(c => c.StageId)
                .HasColumnName("stage_id")
                .IsRequired();

            cleared.Property(c => c.ClearedAt)
                .HasColumnName("cleared_at")
                .IsRequired();

            cleared.HasKey("campaign_progress_id", nameof(ClearedStage.StageId));
        });

        builder.Navigation(x => x.ClearedStages)
            .HasField("_clearedStages")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.DomainEvents);
    }
}
