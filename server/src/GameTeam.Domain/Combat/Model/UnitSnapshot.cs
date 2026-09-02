namespace GameTeam.Domain.Combat.Model;

/// <summary>
/// Ảnh chụp bất biến của một đơn vị đầu trận (combat-framework.md §9). <see cref="ActorId"/> là chuỗi
/// ổn định, <b>duy nhất trong trận</b> — dùng làm tie-break cuối cùng cho mọi thứ tự (§13/§14).
/// </summary>
public sealed record UnitSnapshot(string ActorId, string HeroId, string Team, int Slot, UnitStats Stats);
