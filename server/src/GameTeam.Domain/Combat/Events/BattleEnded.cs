using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>Sự kiện cuối cùng của event log, ngay trước khi dựng <c>result</c> (§18/§19).</summary>
public sealed record BattleEnded : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "BattleEnded";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer)
    {
        // Không có trường riêng.
    }
}
