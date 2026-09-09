using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Contracts.Battle;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Battles;

/// <summary>
/// Đánh một trận (server-authoritative, ADR-011/007): snapshot đội (29) + sinh seed + re-sim (24) + xác định
/// outcome/thưởng + ghi kết quả/cấp thưởng <b>atomic</b> (<see cref="ITransactionalRequest"/> ⇒ TransactionBehavior).
/// <see cref="AttemptId"/> là idempotency key: retry cùng khoá ⇒ trả kết quả đã lưu, KHÔNG cấp thưởng lần hai.
/// Chủ sở hữu suy từ token (không nhận trong command — chống IDOR).
/// </summary>
public sealed record StartBattleCommand(Guid TeamId, string StageId, string AttemptId)
    : IRequest<Result<BattleResultDto>>, ITransactionalRequest;
