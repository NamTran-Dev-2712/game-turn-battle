using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>Kết thúc một vòng.</summary>
public sealed record RoundEnded(int Round) : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "RoundEnded";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer) => writer.WriteNumber("round", Round);
}
