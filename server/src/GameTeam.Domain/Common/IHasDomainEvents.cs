namespace GameTeam.Domain.Common;

/// <summary>
/// Marker <b>không generic</b> để tầng ngoài (Infrastructure) phát hiện &amp; đọc domain event của một
/// aggregate mà không cần biết kiểu định danh <c>TId</c> (không thể truy vấn open-generic
/// <see cref="AggregateRoot{TId}"/> qua EF ChangeTracker).
/// <para>
/// <see cref="AggregateRoot{TId}"/> hiện thực interface này. Domain vẫn <b>chỉ raise &amp; collect</b>;
/// dispatch thuộc Infrastructure (Phase 11) — interface này chỉ mở seam đọc/clear, KHÔNG dispatch trong Domain.
/// </para>
/// BCL-only (không package) — giữ <c>GameTeam.Domain</c> thuần (NetArchTest canh).
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>Các domain event đã raise (chỉ đọc).</summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>Xoá toàn bộ domain event đã thu thập (gọi sau khi dispatch ở tầng ngoài).</summary>
    void ClearDomainEvents();
}
