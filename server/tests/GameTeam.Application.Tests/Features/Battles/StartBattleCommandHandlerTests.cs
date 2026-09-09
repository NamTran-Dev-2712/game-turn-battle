using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Combat;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Combat;
using GameTeam.Application.Features.Battles;
using GameTeam.Application.Tests.Combat;
using GameTeam.Contracts.Battle;
using GameTeam.Domain.Battles;
using GameTeam.Domain.Combat;
using GameTeam.Domain.Combat.Model;
using GameTeam.Domain.Combat.Serialization;
using GameTeam.Domain.Common;
using GameTeam.Domain.Economy;
using GameTeam.Domain.Profiles;
using NSubstitute;
using Xunit;
using DomainTeam = GameTeam.Domain.Teams.Team;
using DomainTeamSlot = GameTeam.Domain.Teams.TeamSlot;

namespace GameTeam.Application.Tests.Features.Battles;

/// <summary>
/// Phase 30 — <see cref="StartBattleCommandHandler"/> (Docker-free, port mock + config giả + sim thật):
/// server re-sim quyết outcome; thưởng cấp bởi server (chỉ VICTORY, chỉ currency) trong luồng transaction;
/// idempotency (retry cùng attemptId ⇒ không cấp thưởng lần hai); chống IDOR (teamId phải khớp); auth.
/// </summary>
public sealed class StartBattleCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.NewGuid();
    private const string StageId = "stage_test";
    private const string AttemptId = "attempt-abc";
    private const long Seed = 42;

    [Fact]
    public async Task Victory_grants_gold_and_persists_record_with_result()
    {
        Harness h = Harness.WinningStage();
        BattleRecord? record = null;
        Wallet? wallet = null;
        await h.BattleRecords.AddAsync(Arg.Do<BattleRecord>(r => record = r), Arg.Any<CancellationToken>());
        await h.Wallets.AddAsync(Arg.Do<Wallet>(w => wallet = w), Arg.Any<CancellationToken>());

        Result<BattleResultDto> result = await h.Handle(new StartBattleCommand(h.TeamId, StageId, AttemptId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Outcome.Should().Be("VICTORY");
        result.Value.Seed.Should().Be(Seed);
        result.Value.Log.Should().Contain("event_log");
        result.Value.Rewards.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new RewardDto("currency", "gold", 100));

        await h.BattleRecords.Received(1).AddAsync(Arg.Any<BattleRecord>(), Arg.Any<CancellationToken>());
        record!.Outcome.Should().Be("VICTORY");
        record.AttemptId.Should().Be(AttemptId);
        wallet!.BalanceOf("gold").Should().Be(100);
    }

    [Fact]
    public async Task Returned_seed_reproduces_the_server_outcome_for_replay()
    {
        Harness h = Harness.WinningStage();

        Result<BattleResultDto> result = await h.Handle(new StartBattleCommand(h.TeamId, StageId, AttemptId));

        // Replay-parity: re-sim với cùng seed + cùng config ⇒ cùng outcome (client dùng đúng cách này).
        BattleInput input = new CombatInputResolver(h.Config)
            .Resolve(new BattleRequest((ulong)result.Value.Seed, StageId, Harness.Ally())).Value;
        BattleOutput replay = new BattleSimulator().Simulate(input);
        replay.Result.Outcome.Should().Be(result.Value.Outcome);
        CombatEventSerializer.Serialize(replay).Should().Be(result.Value.Log);
    }

    [Fact]
    public async Task Retry_with_same_attempt_returns_stored_result_without_regrant()
    {
        Harness h = Harness.WinningStage();
        BattleRecord stored = BattleRecord.Create(
            Guid.NewGuid(), h.Profile.Id, AttemptId, h.TeamId, StageId, Seed, "VICTORY", 1,
            new[] { new BattleReward("currency", "gold", 100) }, "{\"event_log\":[]}", Now);
        h.BattleRecords.GetByProfileAndAttemptAsync(h.Profile.Id, AttemptId, Arg.Any<CancellationToken>())
            .Returns(stored);

        Result<BattleResultDto> result = await h.Handle(new StartBattleCommand(h.TeamId, StageId, AttemptId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Outcome.Should().Be("VICTORY");
        result.Value.Rewards.Should().ContainSingle();
        // KHÔNG re-sim / ghi record mới / credit ví lần hai.
        await h.BattleRecords.DidNotReceive().AddAsync(Arg.Any<BattleRecord>(), Arg.Any<CancellationToken>());
        await h.Wallets.DidNotReceive().AddAsync(Arg.Any<Wallet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Defeat_grants_no_reward()
    {
        Harness h = Harness.LosingStage();

        Result<BattleResultDto> result = await h.Handle(new StartBattleCommand(h.TeamId, StageId, AttemptId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Outcome.Should().Be("DEFEAT");
        result.Value.Rewards.Should().BeEmpty();
        await h.Wallets.DidNotReceive().AddAsync(Arg.Any<Wallet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_when_team_id_does_not_match_callers_team()
    {
        Harness h = Harness.WinningStage();

        Result<BattleResultDto> result = await h.Handle(new StartBattleCommand(Guid.NewGuid(), StageId, AttemptId));

        result.Error.Code.Should().Be("BATTLE_TEAM_NOT_FOUND");
    }

    [Fact]
    public async Task Rejects_unauthenticated()
    {
        Harness h = Harness.WinningStage();
        h.CurrentUser.AccountId.Returns((Guid?)null);

        Result<BattleResultDto> result = await h.Handle(new StartBattleCommand(h.TeamId, StageId, AttemptId));

        result.Error.Code.Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task Rejects_when_no_profile()
    {
        Harness h = Harness.WinningStage();
        h.Profiles.GetByAccountIdAsync(AccountId, Arg.Any<CancellationToken>()).Returns((PlayerProfile?)null);

        Result<BattleResultDto> result = await h.Handle(new StartBattleCommand(h.TeamId, StageId, AttemptId));

        result.Error.Code.Should().Be("PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task Unknown_stage_returns_combat_error()
    {
        Harness h = Harness.WinningStage();

        Result<BattleResultDto> result = await h.Handle(new StartBattleCommand(h.TeamId, "stage_unknown", AttemptId));

        result.Error.Code.Should().Be("COMBAT_STAGE_CONFIG_NOT_FOUND");
    }

    private sealed class Harness
    {
        public IBattleRecordRepository BattleRecords { get; } = Substitute.For<IBattleRecordRepository>();
        public IWalletRepository Wallets { get; } = Substitute.For<IWalletRepository>();
        public ITeamRepository Teams { get; } = Substitute.For<ITeamRepository>();
        public IPlayerProfileRepository Profiles { get; } = Substitute.For<IPlayerProfileRepository>();
        public ICurrentUser CurrentUser { get; } = Substitute.For<ICurrentUser>();
        public IBattleSeedSource SeedSource { get; } = Substitute.For<IBattleSeedSource>();
        public FakeConfigProvider Config { get; } = new();
        public PlayerProfile Profile { get; } = PlayerProfile.CreateForAccount(Guid.NewGuid(), AccountId, Now);
        public Guid TeamId { get; } = Guid.NewGuid();

        public static CombatTeamMember[] Ally() => new[] { new CombatTeamMember("ally_0", "hero_a", 0) };

        public static Harness WinningStage() => Build(allyAtk: 300, allyHp: 1000, enemyAtk: 10, enemyHp: 100);

        public static Harness LosingStage() => Build(allyAtk: 5, allyHp: 40, enemyAtk: 300, enemyHp: 1000);

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
            h.Wallets.GetByProfileIdAsync(h.Profile.Id, Arg.Any<CancellationToken>()).Returns((Wallet?)null);

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
            h.Config.Set("stage", StageId, """
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
            h.Config.Set("reward", "reward_test", """
                { "entries": [ { "reward_type": "currency", "ref_id": "gold", "amount": 100 } ] }
                """);
            return h;
        }

        public Task<Result<BattleResultDto>> Handle(StartBattleCommand command)
        {
            var clock = Substitute.For<IClock>();
            clock.UtcNow.Returns(Now);
            var handler = new StartBattleCommandHandler(
                CurrentUser, Profiles, Teams, Config, new CombatInputResolver(Config), new BattleSimulator(),
                BattleRecords, Wallets, SeedSource, clock);
            return handler.Handle(command, CancellationToken.None);
        }
    }
}
