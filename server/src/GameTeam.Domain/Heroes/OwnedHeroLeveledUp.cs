using GameTeam.Domain.Common;

namespace GameTeam.Domain.Heroes;

/// <summary>
/// Domain event: một <see cref="OwnedHero"/> vừa được nâng lên cấp <see cref="NewLevel"/> (Phase 35,
/// server-authoritative). Aggregate chỉ raise/thu thập; dispatch do Infrastructure lo tại
/// <c>AppDbContext.SaveChangesAsync</c> (sau persist, cùng transaction — dùng cho telemetry/quest sau này).
/// </summary>
/// <param name="OwnedHeroId">Định danh instance hero được nâng.</param>
/// <param name="ProfileId">Profile sở hữu.</param>
/// <param name="HeroId">Id definition hero ở config.</param>
/// <param name="NewLevel">Cấp mới sau khi nâng.</param>
public sealed record OwnedHeroLeveledUp(Guid OwnedHeroId, Guid ProfileId, string HeroId, int NewLevel) : IDomainEvent;
