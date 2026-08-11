namespace GameTeam.Contracts.Enums;

/// <summary>
/// Độ hiếm của hero — ảnh hưởng sức mạnh &amp; giá trị gacha (docs/mvp/12-glossary.md §2).
/// Giá trị số khớp số sao (3★→5★); mở rộng bậc cao hơn là additive ở phase sau.
/// <para>
/// QUY TẮC ỔN ĐỊNH CONTRACT: không đổi/không tái sử dụng giá trị số đã tồn tại; chỉ THÊM giá trị
/// mới (additive); giá trị bỏ đi thì DEPRECATE, không tái dùng số (docs/backend/api-and-versioning.md §4).
/// </para>
/// </summary>
public enum Rarity
{
    /// <summary>Chưa xác định / chưa gán (mặc định an toàn cho contract).</summary>
    None = 0,

    /// <summary>3★.</summary>
    Three = 3,

    /// <summary>4★.</summary>
    Four = 4,

    /// <summary>5★.</summary>
    Five = 5,
}
