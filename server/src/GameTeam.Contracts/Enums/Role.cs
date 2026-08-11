namespace GameTeam.Contracts.Enums;

/// <summary>
/// Vai trò của hero trong đội (docs/mvp/12-glossary.md §2).
/// <para>
/// Lưu ý đặt tên: <see cref="Dps"/> là PascalCase của "DPS" (quy ước tên .NET; chuỗi canonical = "Dps").
/// </para>
/// <para>
/// QUY TẮC ỔN ĐỊNH CONTRACT: không đổi/không tái sử dụng giá trị số đã tồn tại; chỉ THÊM giá trị
/// mới (additive); giá trị bỏ đi thì DEPRECATE, không tái dùng số (docs/backend/api-and-versioning.md §4).
/// </para>
/// </summary>
public enum Role
{
    /// <summary>Chưa xác định / chưa gán (mặc định an toàn cho contract).</summary>
    None = 0,

    /// <summary>Đỡ đòn.</summary>
    Tank = 1,

    /// <summary>Gây sát thương (DPS).</summary>
    Dps = 2,

    /// <summary>Hỗ trợ.</summary>
    Support = 3,

    /// <summary>Hồi máu.</summary>
    Healer = 4,
}
