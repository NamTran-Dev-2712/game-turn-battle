using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>Một modifier chỉ số hết hạn và bị gỡ khỏi <c>unit</c> (§23), phát tại RoundStarted theo thứ tự tất định.</summary>
public sealed record BuffExpired(string Unit, string Source, string Stat) : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "BuffExpired";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer)
    {
        writer.WriteString("unit", Unit);
        writer.WriteString("source", Source);
        writer.WriteString("stat", Stat);
    }
}
