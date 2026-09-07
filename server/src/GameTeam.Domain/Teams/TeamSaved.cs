using GameTeam.Domain.Common;

namespace GameTeam.Domain.Teams;

/// <summary>Raise khi một <see cref="Team"/> được tạo/lưu (server-authoritative). Dispatch ở SaveChanges (phase 11).</summary>
public sealed record TeamSaved(Guid TeamId, Guid ProfileId) : IDomainEvent;
