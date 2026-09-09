namespace GameTeam.Contracts.Battle;

/// <summary>
/// Body <c>POST /api/v1/battles</c>: <b>intent</b> đánh một trận (đội <paramref name="TeamId"/> + màn
/// <paramref name="StageId"/>) kèm <paramref name="AttemptId"/> — <b>idempotency key do client sinh</b>.
/// Server là bên quyết định (snapshot đội, sinh seed, re-sim, cấp thưởng — ADR-011/007); client chỉ đề xuất.
/// Gọi lại cùng <paramref name="AttemptId"/> ⇒ server trả kết quả đã lưu, <b>KHÔNG</b> cấp thưởng lần hai
/// (chống double-grant). Chủ sở hữu suy từ token (KHÔNG nhận trong body — chống IDOR); <paramref name="TeamId"/>
/// phải khớp đội của người gọi.
/// </summary>
/// <param name="TeamId">Id đội hình (từ <c>GET /api/v1/team</c>) — phải thuộc người gọi.</param>
/// <param name="StageId">Id màn chơi ở config (prefix <c>stage_</c>).</param>
/// <param name="AttemptId">Khoá idempotency do client sinh cho một lần đánh; retry cùng khoá = một kết quả.</param>
public sealed record StartBattleRequest(Guid TeamId, string StageId, string AttemptId);
