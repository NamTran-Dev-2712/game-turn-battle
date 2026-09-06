namespace GameTeam.Domain.Combat.Model;

/// <summary>
/// Bộ skill của một đơn vị trong trận (skill-framework.md §23): <see cref="Basic"/> = đòn thường (luôn có),
/// <see cref="Ultimate"/> = chiêu cuối theo năng lượng (tuỳ chọn — §15). Chọn skill mỗi lượt: cast ultimate
/// nếu đủ năng lượng và không còn hồi chiêu, ngược lại dùng basic (data-driven, không hardcode skill).
/// Vắng mặt (<c>null</c> trên <see cref="UnitSnapshot"/>) ⇒ đơn vị dùng basic skill dùng chung của trận
/// (tương thích ngược golden vector phase 24/26).
/// </summary>
public sealed record UnitSkillSet(SkillDef Basic, SkillDef? Ultimate = null);
