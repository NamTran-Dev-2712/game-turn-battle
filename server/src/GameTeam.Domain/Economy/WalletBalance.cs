namespace GameTeam.Domain.Economy;

/// <summary>
/// Số dư một loại tiền tệ trong <see cref="Wallet"/> (một dòng <c>currency → amount</c>). Số dư chỉ tăng ở
/// phase 30 (cấp thưởng); tiêu/giao dịch có ledger là phase 31. Số nguyên không âm (ADR-011).
/// </summary>
public sealed class WalletBalance
{
    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private WalletBalance()
    {
    }

    /// <summary>Dựng một dòng số dư. Guard: currency không rỗng, amount không âm.</summary>
    public WalletBalance(string currency, long amount)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency không được rỗng.", nameof(currency));
        }

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount không được âm.");
        }

        Currency = currency;
        Amount = amount;
    }

    /// <summary>Mã tiền tệ (<c>gold</c>/<c>gem</c>/<c>ticket</c>).</summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>Số dư hiện tại (không âm).</summary>
    public long Amount { get; private set; }

    /// <summary>Cộng thêm vào số dư (chỉ dùng nội bộ <see cref="Wallet.Credit"/>).</summary>
    internal void Add(long delta)
    {
        if (delta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Chỉ cộng (credit) ở phase 30.");
        }

        Amount += delta;
    }
}
