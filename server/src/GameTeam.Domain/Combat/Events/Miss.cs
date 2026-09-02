using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>Đòn trượt (roll hit ≥ accuracy_bp) — không tiêu thụ roll crit, không sát thương (§16).</summary>
public sealed record Miss(string Actor, string Target) : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "Miss";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer)
    {
        writer.WriteString("actor", Actor);
        writer.WriteString("target", Target);
    }
}
