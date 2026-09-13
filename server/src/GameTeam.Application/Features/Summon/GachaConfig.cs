namespace GameTeam.Application.Features.Summon;

/// <summary>
/// POCO đọc banner gacha từ config qua <c>IConfigProvider.Get&lt;GachaConfig&gt;("gacha", id)</c> (data-driven,
/// ADR-004). Khoá JSON là <c>snake_case</c> (map PascalCase↔snake_case bằng naming policy của
/// <c>RuntimeConfigProvider</c>). Bám <c>gacha.schema.json</c> (phase 06 + mở rộng additive phase 33:
/// <c>cost</c>/<c>dupe_fragments</c>/<c>pity.target_rarity</c>). KHÔNG chứa giá trị mặc định — rate/pity/cost là
/// tuning trong config; thiếu config ⇒ null (handler báo lỗi, không đoán). Rarity của một hero đọc từ
/// <c>HeroConfig.Rarity</c>, KHÔNG lưu ở đây.
/// </summary>
public sealed class GachaConfig
{
    /// <summary>Id banner (prefix <c>gacha_</c>).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Danh sách hero trong banner (gacha.pool → hero id).</summary>
    public IReadOnlyList<string> Pool { get; init; } = Array.Empty<string>();

    /// <summary>Tỉ trọng theo rarity (weight tương đối — tuning).</summary>
    public IReadOnlyList<GachaRate> Rates { get; init; } = Array.Empty<GachaRate>();

    /// <summary>Cấu hình pity (tuỳ chọn) — thiếu ⇒ không pity.</summary>
    public GachaPityConfig? Pity { get; init; }

    /// <summary>Giá một lần quay (bắt buộc để banner quay được — handler kiểm).</summary>
    public GachaCostConfig? Cost { get; init; }

    /// <summary>Số mảnh cấp khi trúng hero đã sở hữu, theo rarity (bắt buộc phủ mọi rarity trong rates — handler kiểm).</summary>
    public IReadOnlyList<GachaDupeFragment> DupeFragments { get; init; } = Array.Empty<GachaDupeFragment>();
}

/// <summary>Tỉ trọng một rarity trong banner (weight là tuning).</summary>
public sealed class GachaRate
{
    /// <summary>Độ hiếm (3/4/5).</summary>
    public int Rarity { get; init; }

    /// <summary>Trọng số tương đối (integer &gt;= 0, ADR-011).</summary>
    public int Weight { get; init; }
}

/// <summary>Cấu hình pity của banner (ngưỡng/mục tiêu là tuning).</summary>
public sealed class GachaPityConfig
{
    /// <summary>Bật/tắt pity.</summary>
    public bool Enabled { get; init; }

    /// <summary>Ngưỡng: số lần quay liên tiếp không trúng mục tiêu thì lần kế đảm bảo trúng.</summary>
    public int Threshold { get; init; }

    /// <summary>Rarity được đảm bảo khi chạm ngưỡng (0 ⇒ handler dùng rarity cao nhất trong rates).</summary>
    public int TargetRarity { get; init; }
}

/// <summary>Giá một lần quay (currency + amount là tuning).</summary>
public sealed class GachaCostConfig
{
    /// <summary>Loại tiền (gold/gem/ticket).</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>Số tiền một lần quay (integer &gt;= 0, ADR-011).</summary>
    public long Amount { get; init; }
}

/// <summary>Số mảnh cấp khi trúng trùng, theo rarity (amount là tuning).</summary>
public sealed class GachaDupeFragment
{
    /// <summary>Độ hiếm (3/4/5).</summary>
    public int Rarity { get; init; }

    /// <summary>Số mảnh cấp khi trùng hero rarity này (integer &gt;= 0, ADR-011).</summary>
    public long Amount { get; init; }
}
