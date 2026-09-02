using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>Sát thương cuối (số nguyên, §17) đã trừ vào mục tiêu; <c>target_hp_after</c> là HP còn lại.</summary>
public sealed record DamageApplied(string Actor, string Target, int Amount, int TargetHpAfter, bool Crit)
    : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "DamageApplied";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer)
    {
        writer.WriteString("actor", Actor);
        writer.WriteString("target", Target);
        writer.WriteNumber("amount", Amount);
        writer.WriteNumber("target_hp_after", TargetHpAfter);
        writer.WriteBoolean("crit", Crit);
    }
}
