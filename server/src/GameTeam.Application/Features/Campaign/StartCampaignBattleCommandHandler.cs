using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Combat;
using GameTeam.Application.Features.Battles;
using GameTeam.Contracts.Battle;
using GameTeam.Domain.Battles;
using GameTeam.Domain.Campaign;
using GameTeam.Domain.Common;
using MediatR;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;

namespace GameTeam.Application.Features.Campaign;

/// <summary>
/// Handles <see cref="StartCampaignBattleCommand"/> — luồng campaign server-authoritative (ADR-011/007):
/// <list type="number">
///   <item>chủ sở hữu từ token → profile;</item>
///   <item>validate stage thuộc chuỗi campaign + có config (<c>CAMPAIGN_STAGE_NOT_FOUND</c>);</item>
///   <item><b>anti-skip</b>: stage phải đã mở khoá theo chuỗi tuần tự (<c>CAMPAIGN_STAGE_LOCKED</c>) — kiểm
///     TRƯỚC khi có bất kỳ mutation nào;</item>
///   <item>chạy trận qua <see cref="BattleExecutionService"/> (dùng chung battle flow 30);</item>
///   <item>chỉ khi VICTORY <b>first-clear</b>: cấp thưởng (<see cref="StageRewardService"/>) + cập nhật tiến độ
///     + "current AFK stage" — tất cả trong MỘT transaction (atomic). Clear lại/thua ⇒ không thưởng, không lùi
///     tiến độ.</item>
/// </list>
/// Client chỉ nhận kết quả; tiến độ authoritative refresh qua <see cref="GetCampaignProgressQuery"/>.
/// </summary>
public sealed class StartCampaignBattleCommandHandler
    : IRequestHandler<StartCampaignBattleCommand, Result<BattleResultDto>>
{
    private const string VictoryOutcome = "VICTORY";

    private readonly ICurrentUser _currentUser;
    private readonly IPlayerProfileRepository _profiles;
    private readonly ICampaignProgressRepository _progressRepo;
    private readonly CampaignChain _chain;
    private readonly IConfigProvider _config;
    private readonly BattleExecutionService _battle;
    private readonly StageRewardService _rewards;
    private readonly IClock _clock;

    public StartCampaignBattleCommandHandler(
        ICurrentUser currentUser,
        IPlayerProfileRepository profiles,
        ICampaignProgressRepository progressRepo,
        CampaignChain chain,
        IConfigProvider config,
        BattleExecutionService battle,
        StageRewardService rewards,
        IClock clock)
    {
        _currentUser = currentUser;
        _profiles = profiles;
        _progressRepo = progressRepo;
        _chain = chain;
        _config = config;
        _battle = battle;
        _rewards = rewards;
        _clock = clock;
    }

    public async Task<Result<BattleResultDto>> Handle(
        StartCampaignBattleCommand request, CancellationToken cancellationToken)
    {
        Guid? accountId = _currentUser.AccountId;
        if (accountId is null)
        {
            return CampaignErrors.Unauthenticated;
        }

        DomainProfile? profile = await _profiles.GetByAccountIdAsync(accountId.Value, cancellationToken);
        if (profile is null)
        {
            return CampaignErrors.ProfileNotFound;
        }

        // Stage phải thuộc chuỗi campaign VÀ có config combat (không đoán).
        if (!_chain.IsCampaignStage(request.StageId) ||
            _config.Get<StageCombatConfig>(CombatInputResolver.StageType, request.StageId) is null)
        {
            return Result.Failure<BattleResultDto>(CampaignErrors.StageNotFound(request.StageId));
        }

        CampaignProgress? progress = await _progressRepo.GetByProfileIdAsync(profile.Id, cancellationToken);
        HashSet<string> cleared = CampaignMapping.ClearedSet(progress);

        // Anti-skip (server-authoritative): stage khoá ⇒ trả lỗi TRƯỚC mọi mutation (không sim, không thưởng, không tiến độ).
        if (!_chain.IsUnlocked(request.StageId, cleared))
        {
            return Result.Failure<BattleResultDto>(CampaignErrors.StageLocked(request.StageId));
        }

        // Chạy trận + (VICTORY & first-clear) thưởng + tiến độ, atomic trong transaction.
        return await _battle.ExecuteAsync(
            profile.Id, request.TeamId, request.StageId, request.AttemptId,
            (outcome, innerCt) => GrantAndAdvanceAsync(outcome, request.StageId, profile.Id, progress, cleared, innerCt),
            cancellationToken);
    }

    /// <summary>
    /// Callback thưởng + tiến độ (chạy trong transaction của trận; KHÔNG chạy khi retry idempotent). Chỉ khi
    /// VICTORY và stage <b>chưa clear</b> (first-clear only): cấp thưởng config-driven (khoá idempotency
    /// <c>campaign:{stageId}</c> — replay stage khác attempt vẫn không cấp lần hai) + đánh dấu clear + đặt
    /// "current AFK stage" = stage clear xa nhất. Trả danh sách thưởng để nhúng vào BattleRecord.
    /// </summary>
    private async Task<IReadOnlyList<BattleReward>> GrantAndAdvanceAsync(
        string outcome,
        string stageId,
        Guid profileId,
        CampaignProgress? progress,
        HashSet<string> cleared,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(outcome, VictoryOutcome, StringComparison.Ordinal))
        {
            return [];
        }

        // First-clear only: clear lại ⇒ không cấp lại thưởng, không lùi tiến độ.
        if (progress is not null && progress.IsStageCleared(stageId))
        {
            return [];
        }

        // Khoá idempotency PHẢI scope theo profile (ledger key là unique TOÀN CỤC): first-clear của stage này
        // cho profile này cấp đúng một lần; re-clear cùng profile ⇒ cùng khoá ⇒ không cấp lần hai; profile khác
        // ⇒ khoá khác (không đụng nhau). Chỉ "campaign:{stageId}" sẽ va chạm giữa các profile.
        IReadOnlyList<BattleReward> granted = await _rewards.GrantAsync(
            stageId, profileId, CampaignMapping.CampaignRewardSource, $"campaign:{profileId}:{stageId}", cancellationToken);

        if (progress is null)
        {
            progress = CampaignProgress.Create(Guid.NewGuid(), profileId, _clock.UtcNow);
            await _progressRepo.AddAsync(progress, cancellationToken);
        }

        var newCleared = new HashSet<string>(cleared, StringComparer.Ordinal) { stageId };
        progress.MarkStageCleared(stageId, _chain.NextAfkStageId(newCleared), _clock.UtcNow);
        return granted;
    }
}
