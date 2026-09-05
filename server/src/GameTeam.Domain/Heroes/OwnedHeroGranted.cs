using GameTeam.Domain.Common;

namespace GameTeam.Domain.Heroes;

/// <summary>
/// Domain event: một <see cref="OwnedHero"/> vừa được cấp cho một profile (Phase 27 — hiện là seed tạm khi
/// tạo tài khoản; nhận thật qua summon ở phase 33). Aggregate chỉ raise/thu thập; dispatch do Infrastructure
/// lo tại <c>AppDbContext.SaveChangesAsync</c> (sau persist, cùng transaction).
/// </summary>
/// <param name="OwnedHeroId">Định danh instance hero vừa cấp.</param>
/// <param name="ProfileId">Profile sở hữu.</param>
/// <param name="HeroId">Id definition hero ở config.</param>
public sealed record OwnedHeroGranted(Guid OwnedHeroId, Guid ProfileId, string HeroId) : IDomainEvent;
