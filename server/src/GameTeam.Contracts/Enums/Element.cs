namespace GameTeam.Contracts.Enums;

/// <summary>
/// Hệ nguyên tố của hero — dùng cho khắc chế (docs/mvp/12-glossary.md §2, docs/mvp/03-core-gameplay.md).
/// <para>
/// QUY TẮC ỔN ĐỊNH CONTRACT: không đổi/không tái sử dụng giá trị số đã tồn tại; chỉ THÊM giá trị
/// mới (additive); giá trị bỏ đi thì DEPRECATE, không tái dùng số (docs/backend/api-and-versioning.md §4).
/// </para>
/// </summary>
public enum Element
{
    /// <summary>Chưa xác định / chưa gán (mặc định an toàn cho contract).</summary>
    None = 0,

    /// <summary>Hỏa.</summary>
    Fire = 1,

    /// <summary>Thủy.</summary>
    Water = 2,

    /// <summary>Địa.</summary>
    Earth = 3,

    /// <summary>Quang.</summary>
    Light = 4,

    /// <summary>Ám.</summary>
    Dark = 5,
}
