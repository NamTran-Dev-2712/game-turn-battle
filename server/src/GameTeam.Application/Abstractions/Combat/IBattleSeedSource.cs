namespace GameTeam.Application.Abstractions.Combat;

/// <summary>
/// Port sinh <b>seed trận server-authoritative</b> (ADR-011): server là bên tạo seed, không để client chọn.
/// Trả <b>Int64 không âm</b> để round-trip số nguyên an toàn (client Godot <c>int</c> có dấu). Khai báo ở
/// Application; hiện thực (RNG mật mã) ở Infrastructure — giữ ranh giới tất định của sim (sim nhận seed tường minh).
/// </summary>
public interface IBattleSeedSource
{
    /// <summary>Sinh một seed mới (Int64 không âm) cho một trận.</summary>
    long Next();
}
