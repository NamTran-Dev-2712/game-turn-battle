using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>Năng lượng đơn vị thay đổi (§15). Chỉ phát khi năng lượng được bật (vector mẫu tắt ⇒ không phát).</summary>
public sealed record EnergyChanged(string Unit, int EnergyAfter) : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "EnergyChanged";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer)
    {
        writer.WriteString("unit", Unit);
        writer.WriteNumber("energy_after", EnergyAfter);
    }
}
