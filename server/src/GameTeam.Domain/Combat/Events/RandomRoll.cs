using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>
/// Một lần tiêu thụ RNG (§16). Thứ tự cố định: <c>hit</c> rồi <c>crit</c>; miss = 1 roll, hit = 2 roll
/// (roll crit vẫn tiêu thụ kể cả khi <c>crit_rate_bp==0</c>) — ghi lại để chứng minh stream không lệch.
/// </summary>
public sealed record RandomRoll(string Purpose, int Bound, int Value) : CombatEvent
{
    /// <inheritdoc/>
    public override string Type => "RandomRoll";

    /// <inheritdoc/>
    public override void WriteBody(Utf8JsonWriter writer)
    {
        writer.WriteString("purpose", Purpose);
        writer.WriteNumber("bound", Bound);
        writer.WriteNumber("value", Value);
    }
}
