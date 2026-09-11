using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Contracts.Enums;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Economy.Commands;

/// <summary>
/// Tiêu tiền tệ từ ví của người gọi — server-authoritative (ADR-007). Chủ sở hữu suy TỪ token <c>sub</c>
/// (<c>ICurrentUser</c>), KHÔNG nhận trong payload (chống IDOR). <see cref="ITransactionalRequest"/> ⇒ atomic;
/// <see cref="IdempotencyKey"/> chống double-spend khi retry. Thiếu số dư ⇒ bị từ chối (số dư không đổi).
/// KHÔNG có endpoint HTTP công khai — dùng nội bộ/từ feature khác (gacha 33, shop 40); test qua <c>ISender</c>.
/// </summary>
/// <param name="Currency">Loại tiền tệ tiêu (khác <c>None</c>).</param>
/// <param name="Amount">Số lượng tiêu (&gt; 0).</param>
/// <param name="Source">Sink/lý do cho audit (ví dụ <c>gacha_summon</c>).</param>
/// <param name="IdempotencyKey">Khoá idempotency duy nhất của lần tiêu này.</param>
public sealed record SpendCurrencyCommand(Currency Currency, long Amount, string Source, string IdempotencyKey)
    : IRequest<Result<CurrencyTransactionResult>>, ITransactionalRequest;
