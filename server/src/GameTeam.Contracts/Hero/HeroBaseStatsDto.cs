namespace GameTeam.Contracts.Hero;

/// <summary>
/// Chỉ số nền của một hero definition (đọc từ config — ADR-004). Combat integer (ADR-011). Giá trị thật là
/// tuning ở config, KHÔNG hardcode ở code.
/// </summary>
/// <param name="Hp">Máu nền.</param>
/// <param name="Atk">Tấn công nền.</param>
/// <param name="Def">Phòng thủ nền.</param>
/// <param name="Spd">Tốc độ nền.</param>
public sealed record HeroBaseStatsDto(int Hp, int Atk, int Def, int Spd);
