namespace GameTeam.Contracts.Battle;

/// <summary>
/// Kết quả trận <b>server-authoritative</b> (ADR-011): <paramref name="Seed"/> để client <b>replay</b> bằng
/// sim client (phase 25); <paramref name="Outcome"/>/<paramref name="Rounds"/> do server re-sim quyết định;
/// <paramref name="Rewards"/> do server cấp (client chỉ hiển thị, không tự cấp); <paramref name="Log"/> là
/// event log tất định (định dạng combat dùng chung — <c>shared/combat-vectors</c>) để client vẽ diễn biến +
/// đối chiếu replay ≡ server.
/// <para>
/// <paramref name="Seed"/> là <b>Int64 không âm</b> (server ép bit dấu = 0) để round-trip số nguyên an toàn ở
/// cả C# và client Godot (<c>int</c> 64-bit có dấu) — tránh tràn khi seed &gt; 2^63.
/// </para>
/// </summary>
/// <param name="Seed">Seed PRNG đã dùng (Int64 không âm) — client replay cùng seed ⇒ cùng diễn biến.</param>
/// <param name="Outcome">Kết cục từ góc nhìn ally: <c>VICTORY</c>/<c>DEFEAT</c>/<c>DRAW</c>.</param>
/// <param name="Rounds">Số vòng đã đánh.</param>
/// <param name="Rewards">Các khoản thưởng đã cấp (rỗng nếu không thắng) — server-authoritative.</param>
/// <param name="Log">Event log tất định dạng JSON chuỗi (<c>{event_log, result}</c>) — client parse để vẽ + verify.</param>
public sealed record BattleResultDto(
    long Seed,
    string Outcome,
    int Rounds,
    IReadOnlyList<RewardDto> Rewards,
    string Log);
