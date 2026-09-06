using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>
/// Áp một modifier chỉ số lên <c>unit</c> (§23). <c>source</c> = skill nguồn; <c>stat</c> = atk/def/spd;
/// <c>amount</c> = delta <b>có dấu</b> (buff dương, debuff âm); <c>duration</c> = số vòng hiệu lực.
/// </summary>
public sealed record BuffApplied(string Unit, string Source, string Stat, int Amount, int Duration) : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "BuffApplied";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer)
    {
        writer.WriteString("unit", Unit);
        writer.WriteString("source", Source);
        writer.WriteString("stat", Stat);
        writer.WriteNumber("amount", Amount);
        writer.WriteNumber("duration", Duration);
    }
}
