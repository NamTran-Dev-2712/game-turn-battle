using GameTeam.Application.Abstractions.Combat;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Combat;
using GameTeam.Application.Features.Teams;
using GameTeam.Contracts.Battle;
using GameTeam.Domain.Battles;
using GameTeam.Domain.Combat;
using GameTeam.Domain.Combat.Serialization;
using GameTeam.Domain.Common;
using GameTeam.Domain.Economy;
using MediatR;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;
using DomainTeam = GameTeam.Domain.Teams.Team;

namespace GameTeam.Application.Features.Battles;

/// <summary>
/// Handles <see cref="StartBattleCommand"/> — luồng trận đầu-cuối server-authoritative (ADR-011/007):
/// <list type="number">
///   <item>chủ sở hữu từ token (<see cref="ICurrentUser"/>) → profile;</item>
///   <item><b>idempotency</b>: retry cùng <c>attemptId</c> ⇒ trả kết quả đã lưu (không re-sim/cấp thưởng);</item>
///   <item>snapshot đội (29) — validate <c>teamId</c> thuộc người gọi (chống IDOR);</item>
///   <item>sinh seed server (<see cref="IBattleSeedSource"/>) → dựng input (config) → <b>re-sim</b> (24);</item>
///   <item>thưởng tối giản config-driven: chỉ khi VICTORY, chỉ loại <c>currency</c> → credit ví;</item>
///   <item>ghi <see cref="BattleRecord"/> + credit ví trong <b>một transaction</b> (TransactionBehavior).</item>
/// </list>
/// Client chỉ nhận kết quả + replay bằng seed — không tự quyết outcome/thưởng.
/// </summary>
public sealed class StartBattleCommandHandler : IRequestHandler<StartBattleCommand, Result<BattleResultDto>>
{
    private const string RewardConfigType = "reward";
    private const string CurrencyRewardType = "currency";
    private const string VictoryOutcome = "VICTORY";

    private readonly ICurrentUser _currentUser;
    private readonly IPlayerProfileRepository _profiles;
    private readonly ITeamRepository _teams;
    private readonly IConfigProvider _config;
    private readonly CombatInputResolver _resolver;
    private readonly BattleSimulator _simulator;
    private readonly IBattleRecordRepository _battleRecords;
    private readonly IWalletRepository _wallets;
    private readonly IBattleSeedSource _seedSource;
    private readonly IClock _clock;

    public StartBattleCommandHandler(
        ICurrentUser currentUser,
        IPlayerProfileRepository profiles,
        ITeamRepository teams,
        IConfigProvider config,
        CombatInputResolver resolver,
        BattleSimulator simulator,
        IBattleRecordRepository battleRecords,
        IWalletRepository wallets,
        IBattleSeedSource seedSource,
        IClock clock)
    {
        _currentUser = currentUser;
        _profiles = profiles;
        _teams = teams;
        _config = config;
        _resolver = resolver;
        _simulator = simulator;
        _battleRecords = battleRecords;
        _wallets = wallets;
        _seedSource = seedSource;
        _clock = clock;
    }

    public async Task<Result<BattleResultDto>> Handle(StartBattleCommand request, CancellationToken cancellationToken)
    {
        Guid? accountId = _currentUser.AccountId;
        if (accountId is null)
        {
            return BattleErrors.Unauthenticated;
        }

        DomainProfile? profile = await _profiles.GetByAccountIdAsync(accountId.Value, cancellationToken);
        if (profile is null)
        {
            return BattleErrors.ProfileNotFound;
        }

        // Idempotency (ADR-007): retry cùng attemptId ⇒ trả kết quả đã lưu, KHÔNG re-sim / cấp thưởng lần hai.
        BattleRecord? existing = await _battleRecords.GetByProfileAndAttemptAsync(
            profile.Id, request.AttemptId, cancellationToken);
        if (existing is not null)
        {
            return Result.Success(BattleMapping.ToDto(existing));
        }

        // Team snapshot (server-authoritative) — teamId phải thuộc người gọi (chống IDOR).
        DomainTeam? team = await _teams.GetByProfileIdAsync(profile.Id, cancellationToken);
        if (team is null || team.Id != request.TeamId)
        {
            return BattleErrors.TeamNotFound;
        }

        if (team.Slots.Count == 0)
        {
            return BattleErrors.TeamEmpty;
        }

        IReadOnlyList<CombatTeamMember> ally = TeamSnapshotFactory.Create(team);

        // Server sinh seed (không để client chọn — ADR-011).
        long seed = _seedSource.Next();

        Result<Domain.Combat.Model.BattleInput> resolved =
            _resolver.Resolve(new BattleRequest((ulong)seed, request.StageId, ally));
        if (resolved.IsFailure)
        {
            return Result.Failure<BattleResultDto>(resolved.Error);
        }

        // Re-sim server = nguồn chân lý kết quả (ADR-011).
        BattleOutput output = _simulator.Simulate(resolved.Value);
        string outcome = output.Result.Outcome;
        string logJson = CombatEventSerializer.Serialize(output);

        // Thưởng tối giản config-driven — server cấp, atomic trong transaction (credit ví ↓ và ghi record ↓).
        IReadOnlyList<BattleReward> granted = await GrantRewardsAsync(
            request.StageId, outcome, profile.Id, cancellationToken);

        BattleRecord record = BattleRecord.Create(
            Guid.NewGuid(),
            profile.Id,
            request.AttemptId,
            team.Id,
            request.StageId,
            seed,
            outcome,
            output.Result.Rounds,
            granted,
            logJson,
            _clock.UtcNow);
        await _battleRecords.AddAsync(record, cancellationToken);

        return Result.Success(BattleMapping.ToDto(record));
    }

    /// <summary>
    /// Cấp thưởng tối giản (config-driven): chỉ khi <paramref name="outcome"/> là VICTORY, chỉ entry loại
    /// <c>currency</c> → credit ví (tạo ví nếu chưa có). Trả danh sách khoản đã cấp để lưu vào record + trả client.
    /// Loại thưởng khác (hero/fragment/item) là phase 31–33 (bỏ qua ở đây).
    /// </summary>
    private async Task<IReadOnlyList<BattleReward>> GrantRewardsAsync(
        string stageId, string outcome, Guid profileId, CancellationToken cancellationToken)
    {
        var granted = new List<BattleReward>();
        if (!string.Equals(outcome, VictoryOutcome, StringComparison.Ordinal))
        {
            return granted;
        }

        StageCombatConfig? stage = _config.Get<StageCombatConfig>(CombatInputResolver.StageType, stageId);
        if (stage is null || stage.Rewards.Count == 0)
        {
            return granted;
        }

        Wallet? wallet = null;
        foreach (string rewardId in stage.Rewards)
        {
            RewardConfig? rewardConfig = _config.Get<RewardConfig>(RewardConfigType, rewardId);
            if (rewardConfig is null)
            {
                continue; // referential integrity kiểm ở config-validator (phase 07); an toàn thì bỏ qua.
            }

            foreach (RewardEntryConfig entry in rewardConfig.Entries)
            {
                if (!string.Equals(entry.RewardType, CurrencyRewardType, StringComparison.Ordinal) || entry.Amount <= 0)
                {
                    continue;
                }

                wallet ??= await LoadOrCreateWalletAsync(profileId, cancellationToken);
                wallet.Credit(entry.RefId, entry.Amount, _clock.UtcNow);
                granted.Add(new BattleReward(entry.RewardType, entry.RefId, entry.Amount));
            }
        }

        return granted;
    }

    private async Task<Wallet> LoadOrCreateWalletAsync(Guid profileId, CancellationToken cancellationToken)
    {
        Wallet? wallet = await _wallets.GetByProfileIdAsync(profileId, cancellationToken);
        if (wallet is not null)
        {
            return wallet;
        }

        wallet = Wallet.CreateFor(Guid.NewGuid(), profileId, _clock.UtcNow);
        await _wallets.AddAsync(wallet, cancellationToken);
        return wallet;
    }
}
