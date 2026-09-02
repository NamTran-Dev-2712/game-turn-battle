using System.Text.Json;

namespace GameTeam.Domain.Combat.Events;

/// <summary>
/// Một sự kiện trong event log của trận (combat-framework.md §18). Event log là dòng có thứ tự ổn định;
/// <c>seq</c> tăng từ 0 do <b>vị trí</b> trong danh sách quyết định (serializer gán) ⇒ không thể lệch seq.
/// Mỗi loại tự ghi các trường riêng của mình qua <see cref="WriteBody"/> — tên trường khớp golden format.
/// </summary>
public abstract record CombatEvent
{
    /// <summary>Tên loại sự kiện (khớp golden format — ví dụ <c>DamageApplied</c>).</summary>
    public abstract string Type { get; }

    /// <summary>Ghi các trường riêng của loại này (KHÔNG ghi <c>seq</c>/<c>type</c> — serializer lo).</summary>
    public abstract void WriteBody(Utf8JsonWriter writer);
}
