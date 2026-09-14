using GameTeam.Domain.Common;

namespace GameTeam.Domain.Campaign;

/// <summary>Raise khi tiến độ campaign của một profile được khởi tạo (server-authoritative). Dispatch ở SaveChanges (phase 11).</summary>
public sealed record CampaignProgressCreated(Guid ProgressId, Guid ProfileId) : IDomainEvent;
