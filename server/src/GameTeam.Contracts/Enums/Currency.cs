namespace GameTeam.Contracts.Enums;

/// <summary>
/// Loại tiền tệ MVP (docs/mvp/06-game-economy.md §1, docs/mvp/03-core-gameplay.md §15 — đã chốt ✅).
/// MVP tối thiểu: Gold (soft), Gem (premium), Ticket (vé summon). Các tài nguyên khác
/// (Energy/Fragment…) KHÔNG nằm ở enum này; thêm loại tiền chuyên biệt là additive ở phase sau.
/// <para>
/// QUY TẮC ỔN ĐỊNH CONTRACT: không đổi/không tái sử dụng giá trị số đã tồn tại; chỉ THÊM giá trị
/// mới (additive); giá trị bỏ đi thì DEPRECATE, không tái dùng số (docs/backend/api-and-versioning.md §4).
/// </para>
/// </summary>
public enum Currency
{
    /// <summary>Chưa xác định / chưa gán (mặc định an toàn cho contract).</summary>
    None = 0,

    /// <summary>Soft currency — tiền phổ thông.</summary>
    Gold = 1,

    /// <summary>Premium currency — tiền cao cấp.</summary>
    Gem = 2,

    /// <summary>Summon Ticket — vé rút gacha.</summary>
    Ticket = 3,
}
