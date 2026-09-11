using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Contracts.Enums;
using GameTeam.Domain.Common;
using GameTeam.Domain.Economy;

namespace GameTeam.Application.Features.Economy;

/// <summary>
/// Cơ chế giao dịch tiền tệ <b>tái dùng cho mọi nguồn/sink</b> (battle 30, gacha 33, AFK 37, shop 40, mail 42)
/// — server-authoritative (ADR-007). Một chỗ duy nhất thực hiện: <b>(1)</b> kiểm idempotency (theo
/// <c>idempotency_key</c> unique) → nếu đã xử lý trả lại kết quả cũ, KHÔNG áp dụng lại; <b>(2)</b>
/// <b>khoá dòng ví</b> (<c>FOR UPDATE</c>) để tuần tự hoá thao tác đồng thời (chống lost-update / số dư âm);
/// <b>(3)</b> áp dụng credit/spend (bất biến không âm ở Domain); <b>(4)</b> ghi một dòng <see cref="CurrencyTransaction"/>
/// (audit + idempotency).
/// <para>
/// Service <b>không</b> tự mở transaction: nó chạy trong transaction do <c>TransactionBehavior</c> mở cho
/// command top-level (<c>ITransactionalRequest</c>) — nên gọi từ handler khác (ví dụ battle) là gọi phương
/// thức thường trong cùng transaction, KHÔNG lồng MediatR/transaction. <c>SaveChanges</c>/commit do
/// <c>UnitOfWork</c> lo ⇒ balance + ledger atomic cùng nhau.
/// </para>
/// </summary>
public sealed class CurrencyWalletService
{
    private readonly IWalletRepository _wallets;
    private readonly ICurrencyTransactionRepository _ledger;
    private readonly IClock _clock;

    public CurrencyWalletService(
        IWalletRepository wallets,
        ICurrencyTransactionRepository ledger,
        IClock clock)
    {
        _wallets = Guard.NotNull(wallets);
        _ledger = Guard.NotNull(ledger);
        _clock = Guard.NotNull(clock);
    }

    /// <summary>
    /// Cấp <paramref name="amount"/> (&gt; 0) <paramref name="currency"/> cho ví của <paramref name="profileId"/>,
    /// atomic + idempotent. Retry cùng <paramref name="idempotencyKey"/> ⇒ trả kết quả cũ, không cộng lần hai.
    /// </summary>
    public async Task<Result<CurrencyTransactionResult>> GrantAsync(
        Guid profileId,
        Currency currency,
        long amount,
        string source,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (currency == Currency.None)
        {
            return Result.Failure<CurrencyTransactionResult>(CurrencyErrors.InvalidCurrency);
        }

        // Idempotency: key đã xử lý ⇒ trả kết quả đã lưu, KHÔNG áp dụng lại (chống double-grant).
        CurrencyTransaction? seen = await _ledger.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (seen is not null)
        {
            return Result.Success(ToResult(seen));
        }

        string code = CurrencyCode.ToCode(currency);
        Wallet wallet = await LoadOrCreateForUpdateAsync(profileId, cancellationToken);

        long balanceAfter = wallet.Credit(code, amount, _clock.UtcNow);
        await AppendLedgerAsync(profileId, code, +amount, balanceAfter, source, idempotencyKey, cancellationToken);

        return Result.Success(new CurrencyTransactionResult(currency, +amount, balanceAfter, idempotencyKey));
    }

    /// <summary>
    /// Tiêu <paramref name="amount"/> (&gt; 0) <paramref name="currency"/> từ ví của <paramref name="profileId"/>,
    /// atomic + idempotent. Thiếu số dư ⇒ <c>Result</c> lỗi <see cref="CurrencyErrors.InsufficientFunds"/>,
    /// KHÔNG thay đổi số dư. Retry cùng <paramref name="idempotencyKey"/> ⇒ trả kết quả cũ, không tiêu lần hai.
    /// </summary>
    public async Task<Result<CurrencyTransactionResult>> SpendAsync(
        Guid profileId,
        Currency currency,
        long amount,
        string source,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (currency == Currency.None)
        {
            return Result.Failure<CurrencyTransactionResult>(CurrencyErrors.InvalidCurrency);
        }

        CurrencyTransaction? seen = await _ledger.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (seen is not null)
        {
            return Result.Success(ToResult(seen));
        }

        string code = CurrencyCode.ToCode(currency);
        Wallet wallet = await LoadOrCreateForUpdateAsync(profileId, cancellationToken);

        // Thiếu tiền là lỗi nghiệp vụ mong đợi ⇒ Result, KHÔNG mutate (transaction rollback sạch).
        if (wallet.BalanceOf(code) < amount)
        {
            return Result.Failure<CurrencyTransactionResult>(CurrencyErrors.InsufficientFunds);
        }

        long balanceAfter = wallet.Spend(code, amount, _clock.UtcNow);
        await AppendLedgerAsync(profileId, code, -amount, balanceAfter, source, idempotencyKey, cancellationToken);

        return Result.Success(new CurrencyTransactionResult(currency, -amount, balanceAfter, idempotencyKey));
    }

    private async Task<Wallet> LoadOrCreateForUpdateAsync(Guid profileId, CancellationToken cancellationToken)
    {
        // Khoá dòng ví hiện có (tuần tự hoá thao tác đồng thời). Nếu chưa có ví ⇒ tạo (unique index profile_id
        // là backstop chống tạo trùng khi hai lần cấp đầu tiên chạy song song).
        Wallet? wallet = await _wallets.GetByProfileIdForUpdateAsync(profileId, cancellationToken);
        if (wallet is not null)
        {
            return wallet;
        }

        wallet = Wallet.CreateFor(Guid.NewGuid(), profileId, _clock.UtcNow);
        await _wallets.AddAsync(wallet, cancellationToken);
        return wallet;
    }

    private async Task AppendLedgerAsync(
        Guid profileId,
        string code,
        long delta,
        long balanceAfter,
        string source,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        CurrencyTransaction tx = CurrencyTransaction.Record(
            Guid.NewGuid(), profileId, code, delta, balanceAfter, source, idempotencyKey, _clock.UtcNow);
        await _ledger.AddAsync(tx, cancellationToken);
    }

    private static CurrencyTransactionResult ToResult(CurrencyTransaction tx)
    {
        // Dựng lại kết quả cũ từ dòng ledger đã lưu (mã chuỗi → enum). Mã ledger luôn hợp lệ (do service ghi).
        _ = CurrencyCode.TryParse(tx.Currency, out Currency currency);
        return new CurrencyTransactionResult(currency, tx.Delta, tx.BalanceAfter, tx.IdempotencyKey);
    }
}
