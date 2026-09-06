namespace GameTeam.Domain.Combat.Model;

/// <summary>
/// Chỉ số có thể bị buff/debuff (skill-framework.md §23). Thứ tự khai báo (atk, def, spd) là thứ tự
/// tất định khi phát sự kiện hết hạn buff. Ánh xạ tên (snake_case, khớp config/golden) qua <see cref="StatKinds"/>.
/// </summary>
public enum StatKind
{
    /// <summary>Tấn công.</summary>
    Atk = 0,

    /// <summary>Phòng thủ.</summary>
    Def = 1,

    /// <summary>Tốc độ.</summary>
    Spd = 2,
}

/// <summary>Ánh xạ <see cref="StatKind"/> ↔ tên chuỗi (không switch — tra bảng theo index).</summary>
public static class StatKinds
{
    private static readonly string[] NamesByIndex = { "atk", "def", "spd" };

    /// <summary>Tên chuỗi (snake_case) của một chỉ số — khớp key params config + trường golden.</summary>
    public static string Name(StatKind stat) => NamesByIndex[(int)stat];
}
