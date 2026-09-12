using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Inventory.Commands;

/// <summary>
/// Bớt nhiều item khỏi kho của người gọi — server-authoritative (ADR-007). Chủ sở hữu suy TỪ token <c>sub</c>
/// (<c>ICurrentUser</c>), KHÔNG nhận trong payload (chống IDOR). <see cref="ITransactionalRequest"/> ⇒ atomic:
/// một item thiếu số lượng ⇒ toàn bộ command fail, KHÔNG bớt một phần; <see cref="IdempotencyKey"/> chống
/// double-remove khi retry. KHÔNG có endpoint HTTP công khai — dùng nội bộ/từ feature khác (equipment 38,
/// ascension 39, shop 40); test qua <c>ISender</c>.
/// </summary>
/// <param name="Items">Các khoản bớt (mỗi khoản số lượng &gt; 0).</param>
/// <param name="Source">Nguồn/lý do cho audit (ví dụ <c>ascension</c>).</param>
/// <param name="IdempotencyKey">Khoá idempotency duy nhất của lần bớt này.</param>
public sealed record RemoveItemsCommand(IReadOnlyList<ItemChange> Items, string Source, string IdempotencyKey)
    : IRequest<Result<InventoryTransactionResult>>, ITransactionalRequest;
