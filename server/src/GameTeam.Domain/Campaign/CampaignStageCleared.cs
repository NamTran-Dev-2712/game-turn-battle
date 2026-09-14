using GameTeam.Domain.Common;

namespace GameTeam.Domain.Campaign;

/// <summary>
/// Raise khi một stage campaign được clear LẦN ĐẦU (tiến độ tiến lên — first-clear). Dispatch ở SaveChanges
/// (phase 11). Là hook cho quest/analytics về sau — chưa có subscriber ở Phase 34.
/// </summary>
public sealed record CampaignStageCleared(Guid ProgressId, Guid ProfileId, string StageId) : IDomainEvent;
