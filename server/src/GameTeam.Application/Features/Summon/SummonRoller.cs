using GameTeam.Domain.Combat.Rng;

namespace GameTeam.Application.Features.Summon;

/// <summary>
/// Lõi <b>thuần</b> (không I/O) quyết RNG + rate + pity của một lần triệu hồi — server-authoritative (ADR-011).
/// Tất định theo <c>seed</c> (server sinh), dùng <see cref="Pcg32"/> (một stream/lượt quay). Rate/pity đọc từ
/// <see cref="GachaConfig"/> (data-driven, ADR-004) — KHÔNG nhúng số vào code. Thứ tự tiêu RNG cố định: mỗi lần
/// quay tiêu <b>1 draw rarity rồi 1 draw hero</b> (draw rarity vẫn tiêu kể cả khi pity đè, để tiêu RNG đồng nhất).
/// <para>
/// Pity: <see cref="GachaConfig.Pity"/> bật + threshold &gt; 0 ⇒ khi <c>pity + 1 &gt;= threshold</c> lần quay này
/// được đảm bảo trúng rarity mục tiêu (<c>target_rarity</c> hoặc rarity cao nhất trong rates). Trúng mục tiêu (tự
/// nhiên hoặc do pity) ⇒ reset bộ đếm về 0; ngược lại tăng 1. 10-pull = 10 lần quay tuần tự (pity cập nhật từng lần).
/// </para>
/// </summary>
public static class SummonRoller
{
    /// <summary>Kết quả một lần quay (thuần) — hero + rarity trúng + có phải do pity đảm bảo.</summary>
    public sealed record RolledPull(string HeroId, int Rarity, bool WasPity);

    /// <summary>Kết quả cả lượt quay (thuần) — danh sách pull + bộ đếm pity sau cùng.</summary>
    public sealed record SummonRollOutcome(IReadOnlyList<RolledPull> Pulls, int EndingPity);

    /// <summary>
    /// Quay <paramref name="count"/> lần trên <paramref name="banner"/> với bộ đếm pity ban đầu
    /// <paramref name="startingPity"/> và <paramref name="seed"/> server sinh. <paramref name="heroesByRarity"/>
    /// phải phủ mọi rarity trong rates + rarity mục tiêu (handler kiểm trước khi tiêu tiền). Danh sách hero mỗi
    /// rarity phải theo thứ tự tất định (vd sort id) để tái lập được.
    /// </summary>
    public static SummonRollOutcome Roll(
        GachaConfig banner,
        IReadOnlyDictionary<int, IReadOnlyList<string>> heroesByRarity,
        int startingPity,
        int count,
        ulong seed)
    {
        ArgumentNullException.ThrowIfNull(banner);
        ArgumentNullException.ThrowIfNull(heroesByRarity);

        int targetRarity = ResolveTargetRarity(banner);
        bool pityEnabled = banner.Pity is { Enabled: true, Threshold: > 0 };
        int threshold = banner.Pity?.Threshold ?? 0;

        var rng = new Pcg32(seed);
        int pity = startingPity;
        var pulls = new List<RolledPull>(count);

        for (int i = 0; i < count; i++)
        {
            // Luôn tiêu 1 draw rarity (tiêu RNG đồng nhất), rồi có thể bị pity đè.
            int rolledRarity = RollRarity(banner.Rates, rng);

            bool wasPity = pityEnabled && pity + 1 >= threshold;
            int finalRarity = wasPity ? targetRarity : rolledRarity;

            // Chọn hero đồng đều trong pool của rarity (1 draw).
            IReadOnlyList<string> heroes = heroesByRarity[finalRarity];
            uint heroIndex = rng.Bounded((uint)heroes.Count);
            string heroId = heroes[(int)heroIndex];

            pity = finalRarity == targetRarity ? 0 : pity + 1;
            pulls.Add(new RolledPull(heroId, finalRarity, wasPity));
        }

        return new SummonRollOutcome(pulls, pity);
    }

    /// <summary>Rarity mục tiêu của pity: <c>target_rarity</c> nếu &gt; 0, ngược lại rarity cao nhất trong rates.</summary>
    public static int ResolveTargetRarity(GachaConfig banner)
    {
        int configured = banner.Pity?.TargetRarity ?? 0;
        if (configured > 0)
        {
            return configured;
        }

        int max = 0;
        foreach (GachaRate rate in banner.Rates)
        {
            if (rate.Rarity > max)
            {
                max = rate.Rarity;
            }
        }

        return max;
    }

    // Chọn rarity theo trọng số (weighted): pick trong [0, tổng weight) rồi cộng dồn tìm rarity.
    private static int RollRarity(IReadOnlyList<GachaRate> rates, Pcg32 rng)
    {
        long total = 0;
        foreach (GachaRate rate in rates)
        {
            total += rate.Weight;
        }

        // total == 0 (mọi weight = 0) không hợp lệ — handler đã kiểm. An toàn: trả rarity đầu.
        if (total <= 0)
        {
            return rates[0].Rarity;
        }

        uint pick = rng.Bounded((uint)total);
        long cumulative = 0;
        foreach (GachaRate rate in rates)
        {
            cumulative += rate.Weight;
            if (pick < cumulative)
            {
                return rate.Rarity;
            }
        }

        return rates[^1].Rarity;
    }
}
