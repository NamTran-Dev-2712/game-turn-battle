namespace GameTeam.Domain.Combat.Numerics;

/// <summary>
/// Fixed-point số học <b>xác định</b> cho combat sim (combat-framework.md §10, ADR-011).
/// Một số fixed-point là <see cref="long"/> có dấu biểu diễn (giá trị thực × <see cref="FixedScale"/>).
/// <para>
/// <b>Một luật làm tròn duy nhất — round-half-up</b> — áp tại MỌI <see cref="Mul"/>/<see cref="Div"/>/
/// <see cref="FromFixed"/>. Mọi đại lượng combat là <b>không âm</b>; toán hạng âm là vi phạm hợp đồng
/// (ném exception, KHÔNG bao giờ rơi về float). Chia cho 0 là lỗi logic/config (mẫu số luôn ≥ 1 theo
/// bất biến config <c>K ≥ 1</c>). <b>Cấm tuyệt đối float/double</b> trong toàn bộ đường sim.
/// </para>
/// </summary>
public static class FixedPoint
{
    /// <summary>Hệ số tỉ lệ fixed-point (base-10, 3 chữ số thập phân). <c>1.0 → 1000</c>, <c>1.5 → 1500</c>.</summary>
    public const long FixedScale = 1000L;

    /// <summary>
    /// Làm tròn <c>num/den</c> theo <b>round-half-up</b> (num ≥ 0, den ≥ 1). Dùng chia nguyên cắt về 0:
    /// <c>(num + den/2) / den</c> — đúng vì mọi toán hạng không âm (§10).
    /// </summary>
    public static long RoundHalfUp(long num, long den)
    {
        if (den < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(den), den, "Mẫu số fixed-point phải ≥ 1 (cấm chia 0 — §10).");
        }

        if (num < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(num), num, "Toán hạng fixed-point phải không âm (§10).");
        }

        return (num + (den / 2)) / den;
    }

    /// <summary>Chuyển số nguyên → fixed-point (× <see cref="FixedScale"/>). Yêu cầu không âm.</summary>
    public static long ToFixed(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Giá trị fixed-point phải không âm (§10).");
        }

        return value * FixedScale;
    }

    /// <summary>Chuyển fixed-point → số nguyên (round-half-up về đơn vị 1).</summary>
    public static long FromFixed(long fixedValue) => RoundHalfUp(fixedValue, FixedScale);

    /// <summary>Nhân fixed-point (làm tròn về scale tại toán tử — round-half-up).</summary>
    public static long Mul(long a, long b) => RoundHalfUp(a * b, FixedScale);

    /// <summary>Chia fixed-point (làm tròn về scale tại toán tử — round-half-up). Yêu cầu <paramref name="b"/> ≥ 1.</summary>
    public static long Div(long a, long b) => RoundHalfUp(a * FixedScale, b);

    /// <summary>So sánh hai fixed-point: dấu của <c>a - b</c>.</summary>
    public static int Cmp(long a, long b) => a.CompareTo(b);

    /// <summary>Kẹp <paramref name="x"/> vào [<paramref name="lo"/>, <paramref name="hi"/>].</summary>
    public static long Clamp(long x, long lo, long hi) => x < lo ? lo : (x > hi ? hi : x);
}
