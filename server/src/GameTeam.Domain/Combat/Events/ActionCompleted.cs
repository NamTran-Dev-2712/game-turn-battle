using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>Một đơn vị hoàn tất hành động.</summary>
public sealed record ActionCompleted(string Actor) : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "ActionCompleted";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer) => writer.WriteString("actor", Actor);
}
