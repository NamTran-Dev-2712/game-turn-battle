using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>Một đơn vị chết — phát ngay sau <see cref="DamageApplied"/> làm HP về 0, trước ActionCompleted (§18).</summary>
public sealed record Death(string Unit) : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "Death";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer) => writer.WriteString("unit", Unit);
}
