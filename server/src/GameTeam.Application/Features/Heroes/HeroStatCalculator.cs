using GameTeam.Application.Features.Economy;
using GameTeam.Domain.Combat.Numerics;

namespace GameTeam.Application.Features.Heroes;

/// <summary>
/// Công thức <b>duy nhất</b> tính chỉ số hero theo cấp + Power Rating (Phase 35) — <b>tất định, integer</b>
/// (ADR-011, KHÔNG float) và <b>data-driven</b> (đường cong/tăng trưởng/trọng số từ <see cref="EconomyConfig"/>,
/// ADR-004). Dùng chung bởi luồng nâng cấp (<c>LevelUpHeroCommandHandler</c>) VÀ combat resolver
/// (<c>CombatInputResolver</c>) ⇒ chỉ số vào trận khớp chỉ số hiển thị. Bản mirror phía client
/// (<c>client/src/shared/hero_stats.gd</c>) phải cho cùng kết quả.
/// </summary>
public static class HeroStatCalculator
{
    private const long GrowthDenominatorBp = 10_000L;

    /// <summary>
    /// Chỉ số cấp <paramref name="level"/> = <paramref name="baseStat"/> +
    /// round_half_up(base × growthBp × (level−1) / 10000). Cấp 1 ⇒ đúng chỉ số nền (không tra economy). Yêu cầu
    /// mọi tham số không âm, <paramref name="level"/> ≥ 1 (Guard ở caller/Domain).
    /// </summary>
    public static int ScaleStat(int baseStat, int level, int growthBp)
    {
        if (level <= 1 || growthBp == 0 || baseStat == 0)
        {
            return baseStat;
        }

        long added = FixedPoint.RoundHalfUp((long)baseStat * growthBp * (level - 1), GrowthDenominatorBp);
        return checked((int)(baseStat + added));
    }

    /// <summary>Power Rating = tổng có trọng số của chỉ số cuối (integer). Cơ chế cố định; trọng số từ config.</summary>
    public static int Power(int hp, int atk, int def, int spd, PowerWeightsConfig weights)
        => checked(
            (hp * weights.Hp)
            + (atk * weights.Atk)
            + (def * weights.Def)
            + (spd * weights.Spd));

    /// <summary>
    /// Số cấp tối đa = 1 + độ dài đường cong <c>level_up</c> (mỗi phần tử = một lần lên cấp). Không có đường
    /// cong ⇒ 1 (không thể nâng).
    /// </summary>
    public static int MaxLevel(EconomyConfig economy)
        => 1 + LevelUpCurve(economy).Count;

    /// <summary>
    /// Chi phí (gold) để lên từ <paramref name="currentLevel"/> sang cấp kế = <c>level_up[currentLevel-1]</c>.
    /// Trả <c>false</c> nếu đã ở cấp tối đa (không còn bước) — caller báo lỗi <c>HERO_MAX_LEVEL</c>.
    /// </summary>
    public static bool TryGetLevelUpCost(EconomyConfig economy, int currentLevel, out long cost)
    {
        cost = 0;
        List<int> curve = LevelUpCurve(economy);
        int index = currentLevel - 1;
        if (index < 0 || index >= curve.Count)
        {
            return false;
        }

        cost = curve[index];
        return true;
    }

    private static List<int> LevelUpCurve(EconomyConfig economy)
        => economy.CostCurves.TryGetValue(EconomyConfig.LevelUpCurve, out List<int>? curve) && curve is not null
            ? curve
            : [];
}
