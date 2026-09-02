using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>Đã chọn mục tiêu cho hành động (§14).</summary>
public sealed record TargetSelected(string Actor, string Target) : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "TargetSelected";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer)
    {
        writer.WriteString("actor", Actor);
        writer.WriteString("target", Target);
    }
}
