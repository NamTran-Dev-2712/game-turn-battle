using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>Đòn chí mạng (roll crit &lt; crit_rate_bp) — chỉ phát khi crit; hệ số áp SAU mitigation (§17).</summary>
public sealed record Crit(string Actor, string Target) : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "Crit";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer)
    {
        writer.WriteString("actor", Actor);
        writer.WriteString("target", Target);
    }
}
