using GameTeam.Domain.Combat.Events;
using GameTeam.Domain.Combat.Model;
using GameTeam.Domain.Combat.State;

namespace GameTeam.Domain.Combat.Effects;

/// <summary>
/// Ngữ cảnh khi áp một effect (skill-framework.md). Handler đọc dữ liệu từ đây (attacker/target/skill/
/// rules/crit + params từ config) và phát sự kiện qua <see cref="Emit"/>. Handler <b>không</b> tự sinh
/// ngẫu nhiên ngoài stream — nếu cần RNG, nhận qua ngữ cảnh (chưa cần ở phase 24; effect mẫu tất định).
/// </summary>
public sealed class EffectContext
{
    private readonly List<CombatEvent> _log;

    /// <summary>Đơn vị ra đòn.</summary>
    public UnitState Attacker { get; }

    /// <summary>Mục tiêu.</summary>
    public UnitState Target { get; }

    /// <summary>Skill đang thi triển (chứa hệ số/target rule).</summary>
    public SkillDef Skill { get; }

    /// <summary>Dữ liệu effect hiện tại (effect_type + params).</summary>
    public EffectDef Effect { get; }

    /// <summary>Luật/hằng số combat của trận.</summary>
    public CombatRules Rules { get; }

    /// <summary>Đòn này có chí mạng không (đã roll ở lõi §16).</summary>
    public bool IsCrit { get; }

    /// <summary>Khởi tạo ngữ cảnh effect.</summary>
    public EffectContext(
        UnitState attacker,
        UnitState target,
        SkillDef skill,
        EffectDef effect,
        CombatRules rules,
        bool isCrit,
        List<CombatEvent> log)
    {
        Attacker = attacker;
        Target = target;
        Skill = skill;
        Effect = effect;
        Rules = rules;
        IsCrit = isCrit;
        _log = log;
    }

    /// <summary>Ghi một sự kiện vào event log của trận (thứ tự = thứ tự gọi).</summary>
    public void Emit(CombatEvent combatEvent) => _log.Add(combatEvent);
}
