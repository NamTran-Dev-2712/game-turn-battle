using GameTeam.Application.Abstractions.Combat;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Combat;
using GameTeam.Application.Features.Teams;
using GameTeam.Contracts.Battle;
using GameTeam.Domain.Battles;
using GameTeam.Domain.Combat;
using GameTeam.Domain.Combat.Serialization;
using GameTeam.Domain.Common;
using DomainTeam = GameTeam.Domain.Teams.Team;

namespace GameTeam.Application.Features.Battles;

/// <summary>
/// Cơ chế chạy trận <b>server-authoritative</b> dùng chung (ADR-011/007) — MỘT nguồn sim/record cho mọi luồng
/// trận (battle thường phase 30, campaign phase 34). Thực hiện: <b>(1)</b> idempotency (retry cùng
/// <c>attemptId</c> ⇒ trả kết quả đã lưu, KHÔNG re-sim/cấp thưởng); <b>(2)</b> snapshot đội + validate
/// <c>teamId</c> thuộc người gọi (chống IDOR); <b>(3)</b> sinh seed server → dựng input (config) → <b>re-sim</b>;
/// <b>(4)</b> cấp thưởng qua callback <paramref name="grantRewards"/> do người gọi cung cấp (chính sách khi
/// nào/cấp gì — vd chỉ VICTORY, campaign thêm first-clear); <b>(5)</b> ghi <see cref="BattleRecord"/> nhúng
/// danh sách thưởng. Chạy trong transaction của command top-level (<c>TransactionBehavior</c>) ⇒ record +
/// ledger + mutation của callback atomic cùng nhau. Không tự mở transaction.
/// <para>
/// Callback <b>không</b> được gọi khi retry idempotent (kết quả đã lưu) — nên side-effect của callback (thưởng,
/// tiến độ campaign) cũng idempotent theo <c>attemptId</c>.
/// </para>
/// </summary>
public sealed class BattleExecutionService
{
    private readonly ITeamRepository _teams;
    private readonly IOwnedHeroRepository _ownedHeroes;
    private readonly CombatInputResolver _resolver;
    private readonly BattleSimulator _simulator;
    private readonly IBattleRecordRepository _battleRecords;
    private readonly IBattleSeedSource _seedSource;
    private readonly IClock _clock;

    public BattleExecutionService(
        ITeamRepository teams,
        IOwnedHeroRepository ownedHeroes,
        CombatInputResolver resolver,
        BattleSimulator simulator,
        IBattleRecordRepository battleRecords,
        IBattleSeedSource seedSource,
        IClock clock)
    {
        _teams = Guard.NotNull(teams);
        _ownedHeroes = Guard.NotNull(ownedHeroes);
        _resolver = Guard.NotNull(resolver);
        _simulator = Guard.NotNull(simulator);
        _battleRecords = Guard.NotNull(battleRecords);
        _seedSource = Guard.NotNull(seedSource);
        _clock = Guard.NotNull(clock);
    }

    /// <summary>
    /// Chạy một trận cho <paramref name="profileId"/> với đội <paramref name="teamId"/> ở màn
    /// <paramref name="stageId"/>. <paramref name="grantRewards"/> nhận outcome (VICTORY/DEFEAT/DRAW) và trả
    /// danh sách thưởng đã cấp (đồng thời thực hiện mọi side-effect cùng transaction — vd tiến độ campaign).
    /// Trả <see cref="BattleResultDto"/> đã lưu; retry cùng <paramref name="attemptId"/> ⇒ kết quả đã lưu,
    /// callback KHÔNG chạy.
    /// </summary>
    public async Task<Result<BattleResultDto>> ExecuteAsync(
        Guid profileId,
        Guid teamId,
        string stageId,
        string attemptId,
        Func<string, CancellationToken, Task<IReadOnlyList<BattleReward>>> grantRewards,
        CancellationToken cancellationToken)
    {
        // Idempotency (ADR-007): retry cùng attemptId ⇒ trả kết quả đã lưu, KHÔNG re-sim / cấp thưởng lần hai.
        BattleRecord? existing = await _battleRecords.GetByProfileAndAttemptAsync(profileId, attemptId, cancellationToken);
        if (existing is not null)
        {
            return Result.Success(BattleMapping.ToDto(existing));
        }

        // Team snapshot (server-authoritative) — teamId phải thuộc người gọi (chống IDOR).
        DomainTeam? team = await _teams.GetByProfileIdAsync(profileId, cancellationToken);
        if (team is null || team.Id != teamId)
        {
            return BattleErrors.TeamNotFound;
        }

        if (team.Slots.Count == 0)
        {
            return BattleErrors.TeamEmpty;
        }

        // Cấp hero owned (Phase 35) → chỉ số vào trận tính theo cấp (data-driven). Thiếu ⇒ cấp 1 (nền).
        IReadOnlyList<Domain.Heroes.OwnedHero> owned = await _ownedHeroes.GetByProfileIdAsync(profileId, cancellationToken);
        var levelByHeroId = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Domain.Heroes.OwnedHero hero in owned)
        {
            levelByHeroId[hero.HeroId] = hero.Level;
        }

        IReadOnlyList<CombatTeamMember> ally = TeamSnapshotFactory.Create(team, levelByHeroId);

        // Server sinh seed (không để client chọn — ADR-011).
        long seed = _seedSource.Next();

        Result<Domain.Combat.Model.BattleInput> resolved =
            _resolver.Resolve(new BattleRequest((ulong)seed, stageId, ally));
        if (resolved.IsFailure)
        {
            return Result.Failure<BattleResultDto>(resolved.Error);
        }

        // Re-sim server = nguồn chân lý kết quả (ADR-011).
        BattleOutput output = _simulator.Simulate(resolved.Value);
        string outcome = output.Result.Outcome;
        string logJson = CombatEventSerializer.Serialize(output);

        // Thưởng + side-effect do người gọi quyết (atomic trong transaction).
        IReadOnlyList<BattleReward> granted = await grantRewards(outcome, cancellationToken);

        BattleRecord record = BattleRecord.Create(
            Guid.NewGuid(),
            profileId,
            attemptId,
            team.Id,
            stageId,
            seed,
            outcome,
            output.Result.Rounds,
            granted,
            logJson,
            _clock.UtcNow);
        await _battleRecords.AddAsync(record, cancellationToken);

        return Result.Success(BattleMapping.ToDto(record));
    }
}
