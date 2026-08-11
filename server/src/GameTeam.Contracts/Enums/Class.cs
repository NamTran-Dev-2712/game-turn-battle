namespace GameTeam.Contracts.Enums;

/// <summary>
/// Lớp (phong cách chiến đấu) của hero (docs/mvp/12-glossary.md §2, docs/mvp/03-core-gameplay.md).
/// <para>
/// QUY TẮC ỔN ĐỊNH CONTRACT: không đổi/không tái sử dụng giá trị số đã tồn tại; chỉ THÊM giá trị
/// mới (additive); giá trị bỏ đi thì DEPRECATE, không tái dùng số (docs/backend/api-and-versioning.md §4).
/// </para>
/// </summary>
public enum Class
{
    /// <summary>Chưa xác định / chưa gán (mặc định an toàn cho contract).</summary>
    None = 0,

    /// <summary>Chiến binh — cận chiến, chịu/gây sát thương vật lý.</summary>
    Warrior = 1,

    /// <summary>Pháp sư — sát thương phép.</summary>
    Mage = 2,

    /// <summary>Xạ thủ — sát thương tầm xa.</summary>
    Ranger = 3,

    /// <summary>Hỗ trợ — buff/heal/utility.</summary>
    Support = 4,
}
