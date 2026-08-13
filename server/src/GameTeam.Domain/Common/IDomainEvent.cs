namespace GameTeam.Domain.Common;

/// <summary>
/// Đánh dấu một sự việc nghiệp vụ đã xảy ra trong domain (vd BattleWon, HeroAscended).
/// <para>
/// Domain chỉ <b>raise &amp; thu thập</b> event trong aggregate; <b>dispatch</b> (MediatR/notification/bus)
/// thuộc Application/Infrastructure ở phase 10/11 — KHÔNG dispatch trong Domain.
/// </para>
/// Marker tối giản, không mang timestamp (tránh wall-clock trong Domain — dùng <see cref="IClock"/> khi cần).
/// </summary>
public interface IDomainEvent
{
}
