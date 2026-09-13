using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Contracts.Summon;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Summon;

/// <summary>
/// Triệu hồi (gacha) — server-authoritative (ADR-004/007/011): validate đủ tiền → tiêu tiền (Phase 31,
/// idempotent) → RNG server (seeded) → chọn hero theo rate/pity từ config → cấp hero vào sở hữu (Phase 27) /
/// trùng ⇒ mảnh vào kho (Phase 32); tất cả trong <b>một transaction</b> (<see cref="ITransactionalRequest"/> ⇒
/// TransactionBehavior). <see cref="RequestId"/> là idempotency key: retry cùng khoá ⇒ trả kết quả đã lưu,
/// KHÔNG quay/tiêu/cấp lần hai. Chủ sở hữu suy từ token (không nhận trong command — chống IDOR).
/// </summary>
public sealed record SummonCommand(string BannerId, int Count, string RequestId)
    : IRequest<Result<SummonResultDto>>, ITransactionalRequest;
