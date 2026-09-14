using System;
using System.Linq;
using FluentAssertions;
using GameTeam.Domain.Campaign;
using Xunit;

namespace GameTeam.Domain.Tests.Campaign;

/// <summary>
/// Aggregate <see cref="CampaignProgress"/> (Phase 34): tạo rỗng + event; đánh dấu clear <b>first-clear only</b>
/// (idempotent), đặt "current AFK stage", raise event; guard. Thứ tự stage (config) do Application tính rồi
/// truyền vào — không kiểm ở đây.
/// </summary>
public class CampaignProgressTests
{
    private static readonly Guid ProfileId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_starts_empty_and_raises_event()
    {
        CampaignProgress progress = CampaignProgress.Create(Guid.NewGuid(), ProfileId, Now);

        progress.ProfileId.Should().Be(ProfileId);
        progress.SchemaVersion.Should().Be(CampaignProgress.CurrentSchemaVersion);
        progress.ClearedStages.Should().BeEmpty();
        progress.CurrentAfkStageId.Should().BeEmpty();
        progress.CreatedAt.Should().Be(Now);
        progress.DomainEvents.OfType<CampaignProgressCreated>().Should().ContainSingle();
    }

    [Fact]
    public void MarkStageCleared_first_time_advances_and_sets_afk_and_raises_event()
    {
        CampaignProgress progress = CampaignProgress.Create(Guid.NewGuid(), ProfileId, Now);
        DateTimeOffset later = Now.AddMinutes(5);

        bool first = progress.MarkStageCleared("stage_ch01_01", "stage_ch01_01", later);

        first.Should().BeTrue();
        progress.IsStageCleared("stage_ch01_01").Should().BeTrue();
        progress.ClearedStages.Should().ContainSingle().Which.StageId.Should().Be("stage_ch01_01");
        progress.CurrentAfkStageId.Should().Be("stage_ch01_01");
        progress.UpdatedAt.Should().Be(later);
        progress.DomainEvents.OfType<CampaignStageCleared>().Should().ContainSingle()
            .Which.StageId.Should().Be("stage_ch01_01");
    }

    [Fact]
    public void MarkStageCleared_is_idempotent_no_regress_no_duplicate()
    {
        CampaignProgress progress = CampaignProgress.Create(Guid.NewGuid(), ProfileId, Now);
        progress.MarkStageCleared("stage_ch01_01", "stage_ch01_01", Now);
        progress.MarkStageCleared("stage_ch01_02", "stage_ch01_02", Now);

        // Re-clear an earlier stage: no-op, must NOT regress AFK stage or duplicate the cleared entry.
        bool again = progress.MarkStageCleared("stage_ch01_01", "stage_ch01_01", Now.AddDays(1));

        again.Should().BeFalse();
        progress.ClearedStages.Should().HaveCount(2);
        progress.CurrentAfkStageId.Should().Be("stage_ch01_02", "AFK stage must not regress on re-clear");
    }

    [Fact]
    public void MarkStageCleared_rejects_empty_stage_or_afk()
    {
        CampaignProgress progress = CampaignProgress.Create(Guid.NewGuid(), ProfileId, Now);

        Action emptyStage = () => progress.MarkStageCleared("  ", "stage_x", Now);
        Action emptyAfk = () => progress.MarkStageCleared("stage_x", "  ", Now);

        emptyStage.Should().Throw<ArgumentException>();
        emptyAfk.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Restore_rehydrates_without_event()
    {
        CampaignProgress progress = CampaignProgress.Restore(
            Guid.NewGuid(), ProfileId, [new ClearedStage("stage_ch01_01", Now)], "stage_ch01_01",
            CampaignProgress.CurrentSchemaVersion, Now, Now);

        progress.IsStageCleared("stage_ch01_01").Should().BeTrue();
        progress.CurrentAfkStageId.Should().Be("stage_ch01_01");
        progress.DomainEvents.Should().BeEmpty();
    }
}
