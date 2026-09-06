namespace GameTeam.Application.Combat;

/// <summary>
/// Lát cắt config combat của một skill (data-driven — ADR-004, §23). <c>coeff_fixed</c> là hệ số sát thương
/// fixed-point (1.0 → 1000); <c>effects</c> là danh sách effect-data (effect_type + target? + params);
/// <c>energy_cost</c>/<c>cooldown_rounds</c> dùng cho ultimate (§15). Đây là <b>lát cắt combat</b> nạp qua
/// <see cref="Abstractions.Configuration.IConfigProvider"/> (test/vector/bundle); nối trực tiếp gameplay
/// <c>config/skills/*.json</c> (hero.skills[]) vào trận là phase 30.
/// </summary>
public sealed class SkillCombatConfig
{
    /// <summary>Hệ số sát thương fixed-point.</summary>
    public int CoeffFixed { get; init; }

    /// <summary>Chính sách chọn mục tiêu (§23; vd single_enemy/single_ally/self; mặc định = enemy).</summary>
    public string TargetRule { get; init; } = "default";

    /// <summary>Chi phí năng lượng để cast (ultimate — §15); 0 = đòn thường.</summary>
    public int EnergyCost { get; init; }

    /// <summary>Số vòng hồi chiêu sau khi cast (ultimate — §15); 0 = không hồi chiêu.</summary>
    public int CooldownRounds { get; init; }

    /// <summary>Danh sách effect-data cấu thành skill (rỗng ⇒ mặc định 1 effect <c>damage</c>).</summary>
    public IReadOnlyList<SkillEffectConfig> Effects { get; init; } = new List<SkillEffectConfig>();
}

/// <summary>
/// Một effect trong skill config (data-driven): <c>effect_type</c> định tuyến handler; <c>target</c> (tuỳ chọn)
/// ghi đè target rule của skill; <c>params</c> là tham số tuỳ effect (magnitude/duration...). Số là integer/
/// fixed-point (ADR-011).
/// </summary>
public sealed class SkillEffectConfig
{
    /// <summary>Loại effect (khoá registry).</summary>
    public string EffectType { get; init; } = "damage";

    /// <summary>Ghi đè target rule cho riêng effect này (tuỳ chọn).</summary>
    public string? Target { get; init; }

    /// <summary>Tham số riêng của effect_type (đọc từ config — không hardcode).</summary>
    public IReadOnlyDictionary<string, long> Params { get; init; } =
        new Dictionary<string, long>(StringComparer.Ordinal);
}
