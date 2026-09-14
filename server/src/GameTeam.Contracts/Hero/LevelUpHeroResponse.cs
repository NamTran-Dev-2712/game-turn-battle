namespace GameTeam.Contracts.Hero;

/// <summary>
/// Kết quả nâng cấp một hero (server-authoritative, Phase 35 — ADR-007/011). Client CHỈ hiển thị: cấp mới,
/// chỉ số đã tính lại theo cấp (data-driven từ config), Power Rating, và biến động gold. Client KHÔNG tự tăng
/// cấp/chỉ số/Power hay trừ tiền — mọi giá trị ở đây do server tính.
/// </summary>
/// <param name="HeroId">Id definition hero ở config (khoá ghép với definition).</param>
/// <param name="Level">Cấp mới sau khi nâng.</param>
/// <param name="Stats">Chỉ số cuối theo cấp mới (integer, ADR-011) — dùng cho combat + hiển thị.</param>
/// <param name="Power">Power Rating theo chỉ số cuối (integer, trọng số từ config).</param>
/// <param name="GoldSpent">Gold đã tiêu cho lần nâng này.</param>
/// <param name="GoldBalanceAfter">Số dư gold sau khi tiêu.</param>
public sealed record LevelUpHeroResponse(
    string HeroId,
    int Level,
    HeroBaseStatsDto Stats,
    int Power,
    long GoldSpent,
    long GoldBalanceAfter);
