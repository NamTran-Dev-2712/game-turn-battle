using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Contracts.Battle;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Campaign;

/// <summary>
/// Đánh một stage campaign (server-authoritative, ADR-011/007): validate stage <b>đã mở khoá/tuần tự</b>
/// (chống skip) → chạy trận qua cơ chế dùng chung (battle flow 30) → nếu VICTORY <b>first-clear</b> thì cập nhật
/// tiến độ + "current AFK stage" + cấp thưởng <b>atomic</b> (<see cref="ITransactionalRequest"/> ⇒ TransactionBehavior).
/// <see cref="AttemptId"/> là idempotency key. Chủ sở hữu suy từ token (không nhận trong command — chống IDOR).
/// Trả <see cref="BattleResultDto"/> (dùng lại contract battle); client refresh tiến độ qua
/// <c>GetCampaignProgressQuery</c>.
/// </summary>
public sealed record StartCampaignBattleCommand(Guid TeamId, string StageId, string AttemptId)
    : IRequest<Result<BattleResultDto>>, ITransactionalRequest;
