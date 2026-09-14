using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Combat;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Combat;
using GameTeam.Application.Features.Battles;
using GameTeam.Application.Features.Campaign;
using GameTeam.Application.Features.Economy;
using GameTeam.Application.Tests.Combat;
using GameTeam.Contracts.Battle;
using GameTeam.Contracts.Campaign;
using GameTeam.Domain.Battles;
using GameTeam.Domain.Campaign;
using GameTeam.Domain.Combat;
using GameTeam.Domain.Common;
using GameTeam.Domain.Economy;
using GameTeam.Domain.Profiles;
using NSubstitute;
using Xunit;
using DomainTeam = GameTeam.Domain.Teams.Team;
using DomainTeamSlot = GameTeam.Domain.Teams.TeamSlot;

namespace GameTeam.Application.Tests.Features.Campaign;

/// <summary>
/// Phase 34 — <see cref="StartCampaignBattleCommandHandler"/> + <see cref="GetCampaignProgressQueryHandler"/>
/// (Docker-free, port mock + config giả + sim thật): first-clear thắng ⇒ thưởng + tiến độ + AFK stage; stage
/// khoá ⇒ chặn, KHÔNG mutation (chống skip); stage lạ ⇒ 404; thua ⇒ không tiến độ; re-clear ⇒ không thưởng lần
/// hai (first-clear only); server-authoritative (client không set tiến độ).
/// </summary>
public sealed class StartCampaignBattleCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.NewGuid();
    private const string Stage1 = "stage_ch01_01";
    private const string Stage2 = "stage_ch01_02";
    private const string AttemptId = "attempt-abc";
    private const long Seed = 42;

    [Fact]
    public async Task First_clear_victory_grants_reward_and_advances_progress_and_sets_afk_stage()
    {
        Harness h = Harness.Winning();
        CampaignProgress? saved = null;
        Wallet? wallet = null;
        await h.Progress.AddAsync(Arg.Do<CampaignProgress>(p => saved = p), Arg.Any<CancellationToken>());
        await h.Wallets.AddAsync(Arg.Do<Wallet>(w => wallet = w), Arg.Any<CancellationToken>());

        Result<BattleResultDto> result = await h.Handle(new StartCampaignBattleCommand(h.TeamId, Stage1, AttemptId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Outcome.Should().Be("VICTORY");
        result.Value.Rewards.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new RewardDto("currency", "gold", 100));

        await h.Progress.Received(1).AddAsync(Arg.Any<CampaignProgress>(), Arg.Any<CancellationToken>());
        saved!.IsStageCleared(Stage1).Should().BeTrue();
        saved.CurrentAfkStageId.Should().Be(Stage1);
        wallet!.BalanceOf("gold").Should().Be(100);
    }

    [Fact]
    public async Task Locked_stage_is_rejected_with_no_mutation()
    {
        Harness h = Harness.Winning(); // progress rỗng ⇒ stage2 khoá (chưa clear stage1).

        Result<BattleResultDto> result = await h.Handle(new StartCampaignBattleCommand(h.TeamId, Stage2, AttemptId));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CAMPAIGN_STAGE_LOCKED");
        // Không sim, không thưởng, không tiến độ.
        await h.BattleRecords.DidNotReceive().AddAsync(Arg.Any<BattleRecord>(), Arg.Any<CancellationToken>());
        await h.Progress.DidNotReceive().AddAsync(Arg.Any<CampaignProgress>(), Arg.Any<CancellationToken>());
        await h.Wallets.DidNotReceive().AddAsync(Arg.Any<Wallet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_stage_returns_campaign_stage_not_found()
    {
        Harness h = Harness.Winning();

        Result<BattleResultDto> result = await h.Handle(new StartCampaignBattleCommand(h.TeamId, "stage_ghost", AttemptId));

        result.Error.Code.Should().Be("CAMPAIGN_STAGE_NOT_FOUND");
        await h.BattleRecords.DidNotReceive().AddAsync(Arg.Any<BattleRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Defeat_does_not_advance_progress_or_grant_reward()
    {
        Harness h = Harness.Losing();

        Result<BattleResultDto> result = await h.Handle(new StartCampaignBattleCommand(h.TeamId, Stage1, AttemptId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Outcome.Should().Be("DEFEAT");
        result.Value.Rewards.Should().BeEmpty();
        await h.Progress.DidNotReceive().AddAsync(Arg.Any<CampaignProgress>(), Arg.Any<CancellationToken>());
        await h.Wallets.DidNotReceive().AddAsync(Arg.Any<Wallet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Re_clearing_an_already_cleared_stage_grants_no_second_reward()
    {
        Harness h = Harness.Winning();
        CampaignProgress existing = CampaignProgress.Create(Guid.NewGuid(), h.Profile.Id, Now);
        existing.MarkStageCleared(Stage1, Stage1, Now);
        h.Progress.GetByProfileIdAsync(h.Profile.Id, Arg.Any<CancellationToken>()).Returns(existing);

        Result<BattleResultDto> result = await h.Handle(new StartCampaignBattleCommand(h.TeamId, Stage1, AttemptId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Outcome.Should().Be("VICTORY");
        result.Value.Rewards.Should().BeEmpty("first-clear only — re-clear cấp không thưởng");
        await h.Wallets.DidNotReceive().AddAsync(Arg.Any<Wallet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_unauthenticated()
    {
        Harness h = Harness.Winning();
        h.CurrentUser.AccountId.Returns((Guid?)null);

        Result<BattleResultDto> result = await h.Handle(new StartCampaignBattleCommand(h.TeamId, Stage1, AttemptId));

        result.Error.Code.Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task Progress_query_reports_unlocked_cleared_and_afk_stage()
    {
        Harness h = Harness.Winning();
        CampaignProgress existing = CampaignProgress.Create(Guid.NewGuid(), h.Profile.Id, Now);
        existing.MarkStageCleared(Stage1, Stage1, Now);
        h.Progress.GetByProfileIdAsync(h.Profile.Id, Arg.Any<CancellationToken>()).Returns(existing);

        Result<CampaignProgressDto> result = await h.Query(new GetCampaignProgressQuery());

        result.IsSuccess.Should().BeTrue();
        CampaignProgressDto dto = result.Value;
        dto.CurrentAfkStageId.Should().Be(Stage1);
        dto.Stages.Should().HaveCount(2);
        CampaignStageDto s1 = dto.Stages[0];
        CampaignStageDto s2 = dto.Stages[1];
        s1.StageId.Should().Be(Stage1);
        s1.Cleared.Should().BeTrue();
        s1.Unlocked.Should().BeTrue();
        s2.StageId.Should().Be(Stage2);
        s2.Cleared.Should().BeFalse();
        s2.Unlocked.Should().BeTrue("stage1 đã clear ⇒ stage2 mở khoá");
    }

    private sealed class Harness
    {
        public IBattleRecordRepository BattleRecords { get; } = Substitute.For<IBattleRecordRepository>();
        public IWalletRepository Wallets { get; } = Substitute.For<IWalletRepository>();
        public ICurrencyTransactionRepository Ledger { get; } = Substitute.For<ICurrencyTransactionRepository>();
        public ITeamRepository Teams { get; } = Substitute.For<ITeamRepository>();
        public IPlayerProfileRepository Profiles { get; } = Substitute.For<IPlayerProfileRepository>();
        public ICampaignProgressRepository Progress { get; } = Substitute.For<ICampaignProgressRepository>();
        public ICurrentUser CurrentUser { get; } = Substitute.For<ICurrentUser>();
        public IBattleSeedSource SeedSource { get; } = Substitute.For<IBattleSeedSource>();
        public FakeConfigProvider Config { get; } = new();
        public PlayerProfile Profile { get; } = PlayerProfile.CreateForAccount(Guid.NewGuid(), AccountId, Now);
        public Guid TeamId { get; } = Guid.NewGuid();

        public static Harness Winning() => Build(allyAtk: 300, allyHp: 1000, enemyAtk: 10, enemyHp: 100);

        public static Harness Losing() => Build(allyAtk: 5, allyHp: 40, enemyAtk: 300, enemyHp: 1000);

        private static Harness Build(int allyAtk, int allyHp, int enemyAtk, int enemyHp)
        {
            var h = new Harness();
            h.CurrentUser.AccountId.Returns(AccountId);
            h.Profiles.GetByAccountIdAsync(AccountId, Arg.Any<CancellationToken>()).Returns(h.Profile);
            h.SeedSource.Next().Returns(Seed);

            DomainTeam team = DomainTeam.Create(
                h.TeamId, h.Profile.Id, new[] { new DomainTeamSlot(0, "hero_a") }, Now);
            h.Teams.GetByProfileIdAsync(h.Profile.Id, Arg.Any<CancellationToken>()).Returns(team);
            h.BattleRecords.GetByProfileAndAttemptAsync(h.Profile.Id, AttemptId, Arg.Any<CancellationToken>())
                .Returns((BattleRecord?)null);
            h.Progress.GetByProfileIdAsync(h.Profile.Id, Arg.Any<CancellationToken>()).Returns((CampaignProgress?)null);
            h.Wallets.GetByProfileIdForUpdateAsync(h.Profile.Id, Arg.Any<CancellationToken>()).Returns((Wallet?)null);

            h.Config.Set("chapter", "chapter_01",
                $$"""{ "id": "chapter_01", "order": 1, "stages": ["{{Stage1}}", "{{Stage2}}"] }""");

            h.Config.Set("hero", "hero_a", $$"""
                { "base_stats": { "hp": {{allyHp}}, "atk": {{allyAtk}}, "def": 50, "spd": 120 }, "skills": ["skill_basic"] }
                """);
            h.Config.Set("hero", "hero_dummy", $$"""
                { "base_stats": { "hp": {{enemyHp}}, "atk": {{enemyAtk}}, "def": 10, "spd": 50 }, "skills": ["skill_basic"] }
                """);
            h.Config.Set("skill", "skill_basic", """
                { "target": "single_enemy", "trigger": { "type": "cooldown", "value": 0 },
                  "effects": [ { "effect_type": "damage", "params": { "coeff_fixed": 1000 } } ] }
                """);

            foreach (string stageId in new[] { Stage1, Stage2 })
            {
                h.Config.Set("stage", stageId, """
                    {
                      "max_rounds": 30,
                      "basic_skill_id": "skill_basic",
                      "combat_rules": {
                        "def_constant_k": 300, "min_damage": 1, "crit_multiplier_fixed": 1500,
                        "accuracy_bp": 10000, "crit_rate_bp": 0,
                        "energy": { "initial": 0, "on_attack": 0, "on_hit": 0, "ultimate_cost": 100, "max": 100 }
                      },
                      "enemies": [ { "hero_id": "hero_dummy", "slot": 0 } ],
                      "rewards": ["reward_test"]
                    }
                    """);
            }

            h.Config.Set("reward", "reward_test", """
                { "entries": [ { "reward_type": "currency", "ref_id": "gold", "amount": 100 } ] }
                """);
            return h;
        }

        public Task<Result<BattleResultDto>> Handle(StartCampaignBattleCommand command)
        {
            var clock = Substitute.For<IClock>();
            clock.UtcNow.Returns(Now);
            var wallet = new CurrencyWalletService(Wallets, Ledger, clock);
            var battle = new BattleExecutionService(
                Teams, new CombatInputResolver(Config), new BattleSimulator(), BattleRecords, SeedSource, clock);
            var rewards = new StageRewardService(Config, wallet);
            var handler = new StartCampaignBattleCommandHandler(
                CurrentUser, Profiles, Progress, new CampaignChain(Config), Config, battle, rewards, clock);
            return handler.Handle(command, CancellationToken.None);
        }

        public Task<Result<CampaignProgressDto>> Query(GetCampaignProgressQuery query)
        {
            var handler = new GetCampaignProgressQueryHandler(CurrentUser, Profiles, Progress, new CampaignChain(Config));
            return handler.Handle(query, CancellationToken.None);
        }
    }
}
