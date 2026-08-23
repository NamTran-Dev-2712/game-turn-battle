using GameTeam.Domain.Common;

namespace GameTeam.Domain.Profiles;

/// <summary>
/// Domain event: một <see cref="PlayerProfile"/> vừa được tạo (khởi tạo hồ sơ khi guest login lần đầu —
/// ADR-007, phase 19). Aggregate chỉ raise/thu thập; dispatch do Infrastructure lo tại
/// <c>AppDbContext.SaveChangesAsync</c> (sau persist, cùng transaction).
/// </summary>
/// <param name="ProfileId">Định danh profile vừa tạo.</param>
/// <param name="AccountId">Tài khoản sở hữu profile (quan hệ 1-1).</param>
public sealed record PlayerProfileCreated(Guid ProfileId, Guid AccountId) : IDomainEvent;
