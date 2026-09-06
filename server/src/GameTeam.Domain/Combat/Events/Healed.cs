using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>Hồi máu (§23) đã áp lên <c>unit</c>; <c>amount</c> = lượng hồi thực (sau kẹp MaxHp), <c>target_hp_after</c> = HP còn lại.</summary>
public sealed record Healed(string Unit, int Amount, int TargetHpAfter) : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "Healed";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer)
    {
        writer.WriteString("unit", Unit);
        writer.WriteNumber("amount", Amount);
        writer.WriteNumber("target_hp_after", TargetHpAfter);
    }
}
