using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Features.Heroes;
using GameTeam.Domain.Common;
using GameTeam.Domain.Inventory;

namespace GameTeam.Application.Features.Inventory;

/// <summary>
/// Cơ chế giao dịch kho đồ <b>tái dùng cho mọi nguồn/sink</b> (seed, gacha 33, shop 40, mail 42, equipment 38)
/// — server-authoritative (ADR-007), song sinh với <c>CurrencyWalletService</c> (Phase 31). Một chỗ duy nhất
/// thực hiện: <b>(1)</b> kiểm idempotency (theo <c>idempotency_key</c> unique) → nếu đã xử lý trả lại kết quả
/// cũ, KHÔNG áp dụng lại; <b>(2)</b> kiểm <b>data-driven</b> mọi item theo config (item→catalog, fragment→hero)
/// ⇒ id lạ ⇒ <c>Result</c> lỗi, KHÔNG mutate; <b>(3)</b> <b>khoá dòng kho</b> (<c>FOR UPDATE</c>) để tuần tự
/// hoá thao tác đồng thời (chống lost-update / số lượng âm); <b>(4)</b> áp dụng <b>nhiều item ATOMIC</b> — khi
/// consume, kiểm đủ MỌI item TRƯỚC khi trừ bất kỳ item nào (một item thiếu ⇒ toàn bộ fail, không mutate một
/// phần); <b>(5)</b> ghi MỘT dòng <see cref="InventoryTransaction"/> (audit + idempotency).
/// <para>
/// Service <b>không</b> tự mở transaction: nó chạy trong transaction do <c>TransactionBehavior</c> mở cho
/// command top-level (<c>ITransactionalRequest</c>) — nên gọi từ handler khác (seed, gacha) là gọi phương
/// thức thường trong cùng transaction. <c>SaveChanges</c>/commit do <c>UnitOfWork</c> lo ⇒ kho + ledger atomic.
/// </para>
/// </summary>
public sealed class InventoryService
{
    private readonly IInventoryRepository _inventories;
    private readonly IInventoryTransactionRepository _ledger;
    private readonly IConfigProvider _config;
    private readonly IClock _clock;

    public InventoryService(
        IInventoryRepository inventories,
        IInventoryTransactionRepository ledger,
        IConfigProvider config,
        IClock clock)
    {
        _inventories = Guard.NotNull(inventories);
        _ledger = Guard.NotNull(ledger);
        _config = Guard.NotNull(config);
        _clock = Guard.NotNull(clock);
    }

    /// <summary>
    /// Thêm các item cho kho của <paramref name="profileId"/>, atomic + idempotent. Retry cùng
    /// <paramref name="idempotencyKey"/> ⇒ trả kết quả cũ, không thêm lần hai.
    /// </summary>
    public Task<Result<InventoryTransactionResult>> GrantAsync(
        Guid profileId,
        IReadOnlyList<ItemChange> items,
        string source,
        string idempotencyKey,
        CancellationToken cancellationToken)
        => MutateAsync(profileId, items, InventoryTransaction.GrantDirection, source, idempotencyKey, cancellationToken);

    /// <summary>
    /// Bớt các item khỏi kho của <paramref name="profileId"/>, atomic + idempotent. Thiếu số lượng ở BẤT KỲ
    /// item nào ⇒ <c>Result</c> lỗi <see cref="InventoryErrors.Insufficient"/>, KHÔNG thay đổi kho (không bớt
    /// một phần). Retry cùng <paramref name="idempotencyKey"/> ⇒ trả kết quả cũ, không bớt lần hai.
    /// </summary>
    public Task<Result<InventoryTransactionResult>> ConsumeAsync(
        Guid profileId,
        IReadOnlyList<ItemChange> items,
        string source,
        string idempotencyKey,
        CancellationToken cancellationToken)
        => MutateAsync(profileId, items, InventoryTransaction.ConsumeDirection, source, idempotencyKey, cancellationToken);

    private async Task<Result<InventoryTransactionResult>> MutateAsync(
        Guid profileId,
        IReadOnlyList<ItemChange> items,
        string direction,
        string source,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (items is null || items.Count == 0)
        {
            return Result.Failure<InventoryTransactionResult>(InventoryErrors.UnknownItem);
        }

        // Idempotency: key đã xử lý ⇒ trả kết quả đã lưu, KHÔNG áp dụng lại (chống double-add/double-remove).
        InventoryTransaction? seen = await _ledger.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (seen is not null)
        {
            return Result.Success(ToResult(seen));
        }

        // Gộp trùng (itemType,itemId) để pre-check tổng đúng và ledger một dòng/loại.
        List<ItemChange> merged = Merge(items);

        // Kiểm data-driven MỌI item theo config TRƯỚC khi mutate (item→catalog, fragment→hero). Id lạ ⇒ fail.
        foreach (ItemChange change in merged)
        {
            if (!IsKnownItem(change.ItemType, change.ItemId))
            {
                return Result.Failure<InventoryTransactionResult>(InventoryErrors.UnknownItem);
            }
        }

        Domain.Inventory.Inventory inventory = await LoadOrCreateForUpdateAsync(profileId, cancellationToken);
        DateTimeOffset now = _clock.UtcNow;

        // Consume: kiểm đủ MỌI item TRƯỚC khi trừ bất kỳ (atomic nhiều-item — thiếu 1 ⇒ toàn bộ fail).
        if (direction == InventoryTransaction.ConsumeDirection)
        {
            foreach (ItemChange change in merged)
            {
                if (inventory.QuantityOf(change.ItemType, change.ItemId) < change.Quantity)
                {
                    return Result.Failure<InventoryTransactionResult>(InventoryErrors.Insufficient);
                }
            }
        }

        var changes = new List<InventoryChange>(merged.Count);
        foreach (ItemChange change in merged)
        {
            if (direction == InventoryTransaction.GrantDirection)
            {
                long after = inventory.Add(change.ItemType, change.ItemId, change.Quantity, now);
                changes.Add(new InventoryChange(change.ItemType, change.ItemId, +change.Quantity, after));
            }
            else
            {
                long after = inventory.Remove(change.ItemType, change.ItemId, change.Quantity, now);
                changes.Add(new InventoryChange(change.ItemType, change.ItemId, -change.Quantity, after));
            }
        }

        InventoryTransaction tx = InventoryTransaction.Record(
            Guid.NewGuid(), profileId, direction, changes, source, idempotencyKey, now);
        await _ledger.AddAsync(tx, cancellationToken);

        return Result.Success(ToResult(tx));
    }

    private async Task<Domain.Inventory.Inventory> LoadOrCreateForUpdateAsync(
        Guid profileId, CancellationToken cancellationToken)
    {
        // Khoá dòng kho hiện có (tuần tự hoá thao tác đồng thời). Chưa có kho ⇒ tạo (unique index profile_id là
        // backstop chống tạo trùng khi hai thao tác đầu tiên chạy song song).
        Domain.Inventory.Inventory? inventory =
            await _inventories.GetByProfileIdForUpdateAsync(profileId, cancellationToken);
        if (inventory is not null)
        {
            return inventory;
        }

        inventory = Domain.Inventory.Inventory.CreateFor(Guid.NewGuid(), profileId, _clock.UtcNow);
        await _inventories.AddAsync(inventory, cancellationToken);
        return inventory;
    }

    /// <summary>Item hợp lệ khi loại đúng và id tồn tại trong config (data-driven, ADR-004).</summary>
    private bool IsKnownItem(string itemType, string itemId) => itemType switch
    {
        InventoryItemTypes.Item => _config.GetIds(InventoryItemTypes.ItemConfigType).Contains(itemId),
        InventoryItemTypes.Fragment => _config.GetIds(HeroMapping.ConfigType).Contains(itemId),
        _ => false,
    };

    private static List<ItemChange> Merge(IReadOnlyList<ItemChange> items)
    {
        var byKey = new Dictionary<(string, string), long>();
        var order = new List<(string, string)>();
        foreach (ItemChange item in items)
        {
            (string, string) key = (item.ItemType, item.ItemId);
            if (byKey.TryGetValue(key, out long sum))
            {
                byKey[key] = sum + item.Quantity;
            }
            else
            {
                byKey[key] = item.Quantity;
                order.Add(key);
            }
        }

        return order.Select(k => new ItemChange(k.Item1, k.Item2, byKey[k])).ToList();
    }

    private static InventoryTransactionResult ToResult(InventoryTransaction tx) =>
        new(
            tx.Direction,
            tx.IdempotencyKey,
            tx.Changes.Select(c => new InventoryItemResult(c.ItemType, c.ItemId, c.Delta, c.QuantityAfter)).ToList());
}
