using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>Một đơn vị bắt đầu hành động.</summary>
public sealed record ActionStarted(string Actor) : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "ActionStarted";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer) => writer.WriteString("actor", Actor);
}
