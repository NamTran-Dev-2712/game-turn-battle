using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Contracts.Battle;
using GameTeam.Domain.Battles;
using GameTeam.Domain.Common;
using MediatR;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;

namespace GameTeam.Application.Features.Battles;

/// <summary>
/// Handles <see cref="StartBattleCommand"/> — luồng trận đầu-cuối server-authoritative (ADR-011/007):
/// <list type="number">
///   <item>chủ sở hữu từ token (<see cref="ICurrentUser"/>) → profile;</item>
///   <item>chạy trận qua cơ chế dùng chung <see cref="BattleExecutionService"/> (idempotency + snapshot đội +
///     seed server + re-sim + ghi <see cref="BattleRecord"/>);</item>
///   <item>thưởng tối giản config-driven: chỉ khi VICTORY → cấp qua <see cref="StageRewardService"/>
///     (atomic + idempotent + audit ledger — cơ chế dùng chung Phase 31), khoá <c>battle:{attemptId}</c>.</item>
/// </list>
/// Handler mỏng: mọi việc nặng ở service dùng chung (tái dùng bởi campaign phase 34). Client chỉ nhận kết quả +
/// replay bằng seed — không tự quyết outcome/thưởng.
/// </summary>
public sealed class StartBattleCommandHandler : IRequestHandler<StartBattleCommand, Result<BattleResultDto>>
{
    private const string VictoryOutcome = "VICTORY";
    private const string BattleRewardSource = "battle_reward";

    private readonly ICurrentUser _currentUser;
    private readonly IPlayerProfileRepository _profiles;
    private readonly BattleExecutionService _battle;
    private readonly StageRewardService _rewards;

    public StartBattleCommandHandler(
        ICurrentUser currentUser,
        IPlayerProfileRepository profiles,
        BattleExecutionService battle,
        StageRewardService rewards)
    {
        _currentUser = currentUser;
        _profiles = profiles;
        _battle = battle;
        _rewards = rewards;
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

        // Thưởng: chỉ khi VICTORY, cấp qua cơ chế dùng chung (khoá idempotency battle:{attemptId}). Retry cùng
        // attemptId đã bị BattleExecutionService chặn sớm ⇒ callback không chạy ⇒ không cấp lần hai.
        return await _battle.ExecuteAsync(
            profile.Id, request.TeamId, request.StageId, request.AttemptId,
            (outcome, ct) => string.Equals(outcome, VictoryOutcome, StringComparison.Ordinal)
                ? _rewards.GrantAsync(request.StageId, profile.Id, BattleRewardSource, $"battle:{request.AttemptId}", ct)
                : Task.FromResult<IReadOnlyList<BattleReward>>([]),
            cancellationToken);
    }
}
