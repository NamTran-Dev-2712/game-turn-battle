using System.Runtime.CompilerServices;

namespace GameTeam.Domain.Common;

/// <summary>
/// Helper bảo vệ <b>bất biến</b> (invariant) trong Domain. Vi phạm là lỗi lập trình / đầu vào không hợp lệ
/// ⇒ <b>ném</b> BCL argument exception (chiến lược nhất quán). Lỗi <b>nghiệp vụ mong đợi</b> dùng
/// <see cref="Result"/>, KHÔNG dùng Guard — tránh hai paradigm validate xung đột
/// (docs/backend/domain-and-application.md). Trả lại giá trị đã kiểm để dùng dạng fluent.
/// </summary>
public static class Guard
{
    /// <summary>Bảo đảm tham chiếu khác null; ném <see cref="ArgumentNullException"/> nếu null.</summary>
    public static T NotNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }

    /// <summary>Bảo đảm <paramref name="value"/> &gt; 0; ném <see cref="ArgumentOutOfRangeException"/> nếu không.</summary>
    public static int Positive(
        int value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Giá trị phải dương (> 0).");
        }

        return value;
    }

    /// <inheritdoc cref="Positive(int, string?)"/>
    public static long Positive(
        long value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Giá trị phải dương (> 0).");
        }

        return value;
    }

    /// <summary>
    /// Bảo đảm <paramref name="value"/> nằm trong khoảng đóng [<paramref name="min"/>, <paramref name="max"/>];
    /// ném <see cref="ArgumentOutOfRangeException"/> nếu ngoài khoảng.
    /// </summary>
    public static int InRange(
        int value,
        int min,
        int max,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Giá trị phải trong [{min}, {max}].");
        }

        return value;
    }

    /// <inheritdoc cref="InRange(int, int, int, string?)"/>
    public static long InRange(
        long value,
        long min,
        long max,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Giá trị phải trong [{min}, {max}].");
        }

        return value;
    }
}
