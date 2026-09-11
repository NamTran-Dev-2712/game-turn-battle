using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Contracts.Enums;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Economy.Commands;

/// <summary>
/// Cấp tiền tệ cho ví của người gọi — server-authoritative (ADR-007). Chủ sở hữu suy TỪ token <c>sub</c>
/// (<c>ICurrentUser</c>), KHÔNG nhận trong payload (chống IDOR). <see cref="ITransactionalRequest"/> ⇒ atomic;
/// <see cref="IdempotencyKey"/> chống double-grant khi retry. KHÔNG có endpoint HTTP công khai — dùng nội bộ/
/// từ feature khác (AFK 37, gacha reward…); test qua <c>ISender</c>.
/// </summary>
/// <param name="Currency">Loại tiền tệ cấp (khác <c>None</c>).</param>
/// <param name="Amount">Số lượng cấp (&gt; 0).</param>
/// <param name="Source">Nguồn/lý do cho audit (ví dụ <c>afk_claim</c>).</param>
/// <param name="IdempotencyKey">Khoá idempotency duy nhất của lần cấp này.</param>
public sealed record GrantCurrencyCommand(Currency Currency, long Amount, string Source, string IdempotencyKey)
    : IRequest<Result<CurrencyTransactionResult>>, ITransactionalRequest;
