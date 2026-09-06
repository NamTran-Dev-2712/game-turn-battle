namespace GameTeam.Domain.Combat.Model;

/// <summary>
/// Ảnh chụp bất biến của một đơn vị đầu trận (combat-framework.md §9). <see cref="ActorId"/> là chuỗi
/// ổn định, <b>duy nhất trong trận</b> — dùng làm tie-break cuối cùng cho mọi thứ tự (§13/§14).
/// <see cref="Skills"/> (tuỳ chọn, §23) gán bộ skill riêng cho đơn vị; <c>null</c> ⇒ dùng basic skill
/// dùng chung của trận (tương thích ngược golden vector phase 24/26).
/// </summary>
public sealed record UnitSnapshot(
    string ActorId,
    string HeroId,
    string Team,
    int Slot,
    UnitStats Stats,
    UnitSkillSet? Skills = null);
