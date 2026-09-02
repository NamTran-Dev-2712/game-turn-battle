using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>Đòn trúng (roll hit &lt; accuracy_bp) — sau đó tiêu thụ roll crit (§16).</summary>
public sealed record Hit(string Actor, string Target) : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "Hit";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer)
    {
        writer.WriteString("actor", Actor);
        writer.WriteString("target", Target);
    }
}
