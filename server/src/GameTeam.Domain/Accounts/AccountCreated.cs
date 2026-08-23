using GameTeam.Domain.Common;

namespace GameTeam.Domain.Accounts;

/// <summary>
/// Domain event: một <see cref="Account"/> vừa được tạo. Aggregate chỉ raise/thu thập; dispatch do
/// Infrastructure lo tại <c>AppDbContext.SaveChangesAsync</c> (sau persist, cùng transaction — ADR-007).
/// Là điểm móc để phase sau khởi tạo hồ sơ người chơi idempotent (profile @19) khi guest login lần đầu.
/// </summary>
/// <param name="AccountId">Định danh tài khoản vừa tạo.</param>
/// <param name="Type">Loại tài khoản (MVP: <see cref="AccountType.Guest"/>).</param>
public sealed record AccountCreated(Guid AccountId, AccountType Type) : IDomainEvent;
