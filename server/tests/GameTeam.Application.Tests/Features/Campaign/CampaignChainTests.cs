using System.Collections.Generic;
using FluentAssertions;
using GameTeam.Application.Features.Campaign;
using GameTeam.Application.Tests.Combat;
using Xunit;

namespace GameTeam.Application.Tests.Features.Campaign;

/// <summary>
/// <see cref="CampaignChain"/> (Phase 34): thứ tự chuỗi từ chapter config (order rồi stages), luật mở khoá
/// <b>tuần tự</b> (chống skip), và "current AFK stage" = stage clear xa nhất. Data-driven (đổi config ⇒ đổi
/// chuỗi, không sửa code).
/// </summary>
public sealed class CampaignChainTests
{
    private static CampaignChain Chain()
    {
        var config = new FakeConfigProvider();
        // Nạp lệch thứ tự để chứng minh sort theo 'order' (không theo thứ tự nạp/GetIds).
        config.Set("chapter", "chapter_02", """{ "id": "chapter_02", "order": 2, "stages": ["stage_c"] }""");
        config.Set("chapter", "chapter_01", """{ "id": "chapter_01", "order": 1, "stages": ["stage_a", "stage_b"] }""");
        return new CampaignChain(config);
    }

    private static HashSet<string> Cleared(params string[] ids) => new(ids, System.StringComparer.Ordinal);

    [Fact]
    public void OrderedStageIds_follows_chapter_order_then_stage_order()
    {
        Chain().OrderedStageIds().Should().Equal("stage_a", "stage_b", "stage_c");
    }

    [Fact]
    public void IsCampaignStage_true_only_for_stages_in_the_chain()
    {
        CampaignChain chain = Chain();
        chain.IsCampaignStage("stage_a").Should().BeTrue();
        chain.IsCampaignStage("stage_unknown").Should().BeFalse();
    }

    [Fact]
    public void First_stage_is_unlocked_others_require_predecessor_cleared()
    {
        CampaignChain chain = Chain();

        chain.IsUnlocked("stage_a", Cleared()).Should().BeTrue();          // đầu chuỗi
        chain.IsUnlocked("stage_b", Cleared()).Should().BeFalse();          // chưa clear stage_a
        chain.IsUnlocked("stage_b", Cleared("stage_a")).Should().BeTrue();
        chain.IsUnlocked("stage_c", Cleared("stage_a")).Should().BeFalse(); // skip stage_b
        chain.IsUnlocked("stage_c", Cleared("stage_a", "stage_b")).Should().BeTrue();
        chain.IsUnlocked("stage_unknown", Cleared("stage_a", "stage_b", "stage_c")).Should().BeFalse();
    }

    [Fact]
    public void NextAfkStageId_is_furthest_cleared_in_chain_order()
    {
        CampaignChain chain = Chain();

        chain.NextAfkStageId(Cleared()).Should().BeEmpty();
        chain.NextAfkStageId(Cleared("stage_a")).Should().Be("stage_a");
        chain.NextAfkStageId(Cleared("stage_a", "stage_b")).Should().Be("stage_b");
        // Kể cả tập cleared "lộn xộn", AFK là stage xa nhất theo thứ tự chuỗi.
        chain.NextAfkStageId(Cleared("stage_c", "stage_a")).Should().Be("stage_c");
    }
}
