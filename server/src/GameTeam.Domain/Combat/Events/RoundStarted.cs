using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>Bắt đầu một vòng.</summary>
public sealed record RoundStarted(int Round) : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "RoundStarted";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer) => writer.WriteNumber("round", Round);
}
