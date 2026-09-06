using GameTeam.Domain.Combat.Model;

namespace GameTeam.Domain.Combat.State;

/// <summary>
/// Một hiệu chỉnh chỉ số đang hoạt động (buff/debuff — skill-framework.md §23). <see cref="Amount"/> là
/// delta <b>có dấu</b> (buff dương, debuff âm) cộng vào chỉ số nền. Khoá định danh = (<see cref="SourceSkillId"/>,
/// <see cref="Stat"/>): áp lại cùng khoá ⇒ refresh (thay amount + duration, không chồng vô hạn).
/// <see cref="RemainingRounds"/> giảm 1 mỗi RoundStarted, hết ở 0.
/// </summary>
public sealed class StatModifier
{
    /// <summary>Skill nguồn (khoá refresh + trường <c>source</c> của sự kiện).</summary>
    public string SourceSkillId { get; }

    /// <summary>Chỉ số bị tác động.</summary>
    public StatKind Stat { get; }

    /// <summary>Delta có dấu cộng vào chỉ số nền.</summary>
    public int Amount { get; set; }

    /// <summary>Số vòng còn hiệu lực.</summary>
    public int RemainingRounds { get; set; }

    /// <summary>Khởi tạo một modifier.</summary>
    public StatModifier(string sourceSkillId, StatKind stat, int amount, int remainingRounds)
    {
        SourceSkillId = sourceSkillId;
        Stat = stat;
        Amount = amount;
        RemainingRounds = remainingRounds;
    }
}
