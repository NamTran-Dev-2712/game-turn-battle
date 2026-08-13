namespace GameTeam.Domain.Common;

/// <summary>
/// Base cho entity có định danh. So sánh theo <b>định danh</b> (<see cref="Id"/> + kiểu runtime),
/// không phải theo giá trị thuộc tính. Không mang annotation persistence, không audit field (thêm ở phase feature nếu cần).
/// </summary>
/// <typeparam name="TId">Kiểu định danh (không null).</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>Khởi tạo entity với định danh.</summary>
    protected Entity(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    protected Entity()
    {
    }

    /// <summary>Định danh của entity.</summary>
    public TId Id { get; protected init; } = default!;

    /// <inheritdoc />
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetType() == other.GetType()
            && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>So sánh định danh hai entity.</summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    /// <summary>Nghịch đảo <see cref="operator=="/>.</summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
