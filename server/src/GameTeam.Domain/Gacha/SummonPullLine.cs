namespace GameTeam.Domain.Gacha;

/// <summary>
/// Kết quả bất biến của MỘT lần quay trong một <see cref="SummonRecord"/> — lưu để trả lại nguyên vẹn khi
/// retry idempotent (không quay lại). Hero mới ⇒ <see cref="IsNew"/> true, <see cref="Fragments"/> = 0; trùng
/// hero đã sở hữu ⇒ <see cref="IsNew"/> false, <see cref="Fragments"/> &gt; 0. Số là integer (ADR-011).
/// </summary>
public sealed class SummonPullLine
{
    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private SummonPullLine()
    {
    }

    /// <summary>Dựng một dòng kết quả quay. Guard: heroId không rỗng, rarity &gt; 0, fragments &gt;= 0.</summary>
    public SummonPullLine(string heroId, int rarity, bool isNew, long fragments)
    {
        if (string.IsNullOrWhiteSpace(heroId))
        {
            throw new ArgumentException("HeroId không được rỗng.", nameof(heroId));
        }

        if (rarity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Rarity phải dương.");
        }

        if (fragments < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fragments), fragments, "Fragments không được âm.");
        }

        HeroId = heroId;
        Rarity = rarity;
        IsNew = isNew;
        Fragments = fragments;
    }

    /// <summary>Hero trúng (id config <c>hero_*</c>).</summary>
    public string HeroId { get; private set; } = string.Empty;

    /// <summary>Độ hiếm hero trúng (3/4/5).</summary>
    public int Rarity { get; private set; }

    /// <summary>Hero mới (đã cấp vào sở hữu) hay trùng (chuyển mảnh).</summary>
    public bool IsNew { get; private set; }

    /// <summary>Số mảnh đã cấp nếu trùng (0 nếu là hero mới).</summary>
    public long Fragments { get; private set; }
}
