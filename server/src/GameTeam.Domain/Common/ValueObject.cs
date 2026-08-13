namespace GameTeam.Domain.Common;

/// <summary>
/// Base cho value object: bất biến, so sánh <b>theo giá trị</b> qua các thành phần khai báo ở
/// <see cref="GetEqualityComponents"/>. Cùng thành phần ⇒ bằng nhau; khác ⇒ không bằng; hash nhất quán với equality.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>Các thành phần định nghĩa danh tính giá trị (theo thứ tự, cho phép null).</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public bool Equals(ValueObject? other)
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
            && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = default;
        foreach (object? component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    /// <summary>So sánh giá trị hai value object.</summary>
    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    /// <summary>Nghịch đảo <see cref="operator=="/>.</summary>
    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);
}
