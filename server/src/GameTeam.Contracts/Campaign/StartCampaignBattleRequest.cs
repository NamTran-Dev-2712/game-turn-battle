namespace GameTeam.Contracts.Campaign;

/// <summary>
/// Body <c>POST /api/v1/campaign/battles</c>: <b>intent</b> đánh một stage campaign (đội
/// <paramref name="TeamId"/> + màn <paramref name="StageId"/>) kèm <paramref name="AttemptId"/> — idempotency
/// key do client sinh. Server là bên quyết (validate stage đã mở khoá/tuần tự, snapshot đội, sinh seed, re-sim,
/// cập nhật tiến độ + cấp thưởng first-clear atomic — ADR-011/007). Client KHÔNG gửi tiến độ/độ mở khoá; chủ sở
/// hữu suy từ token (chống IDOR); trả về <c>BattleResultDto</c> (client refresh tiến độ qua
/// <c>GET /api/v1/campaign/progress</c>).
/// </summary>
/// <param name="TeamId">Id đội hình (từ <c>GET /api/v1/team</c>) — phải thuộc người gọi.</param>
/// <param name="StageId">Id stage campaign ở config (prefix <c>stage_</c>) — phải đã mở khoá.</param>
/// <param name="AttemptId">Khoá idempotency do client sinh; retry cùng khoá = một kết quả.</param>
public sealed record StartCampaignBattleRequest(Guid TeamId, string StageId, string AttemptId);
