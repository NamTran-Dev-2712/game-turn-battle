using System.Buffers;
using System.Text;
using System.Text.Json;

namespace GameTeam.Domain.Combat.Serialization;

/// <summary>
/// Serialize <see cref="BattleOutput"/> ra JSON <b>chuẩn tắc, tất định</b> (combat-framework.md §18) —
/// khớp golden format: <c>{ "event_log": [...], "result": {...} }</c>. Chỉ số nguyên/chuỗi/bool; <c>seq</c>
/// gán theo vị trí (0..n-1); <c>final_hp</c> theo thứ tự ổn định (ally trước, enemy sau). Cùng output ⇒
/// cùng chuỗi byte (dùng cho test byte-identical N lần).
/// </summary>
public static class CombatEventSerializer
{
    /// <summary>Serialize output thành chuỗi JSON compact, tất định.</summary>
    public static string Serialize(BattleOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();

            writer.WriteStartArray("event_log");
            for (int seq = 0; seq < output.EventLog.Count; seq++)
            {
                Events.CombatEvent combatEvent = output.EventLog[seq];
                writer.WriteStartObject();
                writer.WriteNumber("seq", seq);
                writer.WriteString("type", combatEvent.Type);
                combatEvent.WriteBody(writer);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartObject("result");
            writer.WriteString("outcome", output.Result.Outcome);
            if (output.Result.WinnerTeam is null)
            {
                writer.WriteNull("winner_team");
            }
            else
            {
                writer.WriteString("winner_team", output.Result.WinnerTeam);
            }

            writer.WriteNumber("rounds", output.Result.Rounds);

            writer.WriteStartObject("final_hp");
            foreach (KeyValuePair<string, int> entry in output.Result.FinalHp)
            {
                writer.WriteNumber(entry.Key, entry.Value);
            }

            writer.WriteEndObject();

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
