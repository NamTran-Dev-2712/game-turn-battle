namespace GameTeam.Application.Combat;

/// <summary>
/// Lát cắt config combat của một skill — đọc <b>trực tiếp từ gameplay skill config</b>
/// (<c>skill.schema.json</c>, phase 06/28) qua <see cref="Abstractions.Configuration.IConfigProvider"/>
/// (data-driven — ADR-004, §23). <c>target</c> = chính sách chọn mục tiêu; <c>trigger.type=energy</c> ⇒
/// <c>trigger.value</c> là chi phí năng lượng cast ultimate (§15); <c>cooldown</c> = số vòng hồi chiêu;
/// <c>effects[]</c> là effect-data (hệ số sát thương nằm ở <c>effects[].params.coeff_fixed</c>).
/// Việc nâng coeff của effect <c>damage</c> lên cấp skill (<see cref="Domain.Combat.Model.SkillDef"/>) do
/// <see cref="CombatInputResolver"/> thực hiện (bám cách sim tiêu thụ <c>Skill.CoeffFixed</c>).
/// </summary>
public sealed class SkillCombatConfig
{
    /// <summary>Chính sách chọn mục tiêu (§23; vd single_enemy/single_ally/self; mặc định = enemy).</summary>
    public string Target { get; init; } = "single_enemy";

    /// <summary>Điều kiện kích hoạt (energy/cooldown). <c>type=energy</c> ⇒ <c>value</c> = energy_cost cast (§15).</summary>
    public SkillTriggerConfig Trigger { get; init; } = new();

    /// <summary>Số vòng hồi chiêu sau khi cast (ultimate — §15); 0 = không hồi chiêu.</summary>
    public int Cooldown { get; init; }

    /// <summary>Danh sách effect-data cấu thành skill (rỗng ⇒ mặc định 1 effect <c>damage</c>).</summary>
    public IReadOnlyList<SkillEffectConfig> Effects { get; init; } = new List<SkillEffectConfig>();
}

/// <summary>Điều kiện kích hoạt skill (<c>trigger</c> trong skill config).</summary>
public sealed class SkillTriggerConfig
{
    /// <summary>Cơ chế kích hoạt: <c>energy</c> hoặc <c>cooldown</c>.</summary>
    public string Type { get; init; } = "cooldown";

    /// <summary>Giá trị kích hoạt (khi <c>type=energy</c> là chi phí năng lượng để cast).</summary>
    public int Value { get; init; }
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
