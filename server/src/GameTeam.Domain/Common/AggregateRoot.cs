namespace GameTeam.Domain.Common;

/// <summary>
/// Base cho aggregate root: ranh giới nhất quán, <b>sở hữu</b> domain event. Kế thừa định danh của
/// <see cref="Entity{TId}"/>. Chỉ raise &amp; thu thập event; dispatch do Application/Infrastructure lo (phase 10/11).
/// </summary>
/// <typeparam name="TId">Kiểu định danh (không null).</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Khởi tạo aggregate với định danh.</summary>
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Các domain event đã raise (chỉ đọc). Trả về wrapper read-only để bên ngoài
    /// KHÔNG thể ép kiểu về <see cref="List{T}"/> và sửa đổi trực tiếp.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Raise (thu thập) một domain event vào aggregate.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>Xoá toàn bộ domain event đã thu thập (gọi sau khi dispatch ở tầng ngoài).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
