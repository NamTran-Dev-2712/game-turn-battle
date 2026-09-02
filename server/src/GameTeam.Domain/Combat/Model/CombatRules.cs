namespace GameTeam.Domain.Combat.Model;

/// <summary>
/// Luật/hằng số combat cho trận (combat-framework.md §16/§17/§19). Toàn bộ là <b>số nguyên combat_int</b>
/// đọc từ config (ADR-004) — sim KHÔNG hardcode. Hệ số fixed-point lưu theo đơn vị FixedScale
/// (<c>crit_multiplier_fixed</c> 1.5 → 1500). Tỉ lệ theo basis point [0..10000].
/// </summary>
public sealed record CombatRules(
    int DefConstantK,
    int MinDamage,
    int CritMultiplierFixed,
    int AccuracyBp,
    int CritRateBp,
    int MaxRounds,
    EnergyRules Energy);
