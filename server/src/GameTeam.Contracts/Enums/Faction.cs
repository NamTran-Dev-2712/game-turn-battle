namespace GameTeam.Contracts.Enums;

/// <summary>
/// Phe của hero — nền tảng khắc chế/synergy (docs/mvp/12-glossary.md §2).
/// <para>
/// Danh sách phe cụ thể (GP2) CHƯA chốt trong SSOT (docs/mvp/10-open-questions.md) → hiện chỉ có
/// sentinel <see cref="None"/>. Thành viên thật được thêm ĐÚNG phase khi GP2 được chốt.
/// </para>
/// <para>
/// QUY TẮC ỔN ĐỊNH CONTRACT: không đổi/không tái sử dụng giá trị số đã tồn tại; chỉ THÊM giá trị
/// mới (additive); giá trị bỏ đi thì DEPRECATE, không tái dùng số (docs/backend/api-and-versioning.md §4).
/// </para>
/// </summary>
public enum Faction
{
    /// <summary>Chưa xác định / chưa gán (mặc định an toàn cho contract).</summary>
    None = 0,
}
