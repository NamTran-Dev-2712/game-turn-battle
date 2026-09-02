namespace GameTeam.Domain.Combat;

/// <summary>
/// Kết quả trận đánh giá từ góc nhìn đội <c>ally</c> (combat-framework.md §19). <see cref="Outcome"/> ∈
/// {<c>VICTORY</c>, <c>DEFEAT</c>, <c>DRAW</c>}; <see cref="WinnerTeam"/> = <c>ally</c>/<c>enemy</c>/<c>null</c>.
/// <see cref="FinalHp"/> ánh xạ actor_id → HP cuối (thứ tự ổn định: ally trước, enemy sau).
/// </summary>
public sealed record BattleResult(
    string Outcome,
    string? WinnerTeam,
    int Rounds,
    IReadOnlyList<KeyValuePair<string, int>> FinalHp);
