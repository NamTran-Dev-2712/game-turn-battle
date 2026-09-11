namespace GameTeam.Domain.Economy;

/// <summary>
/// Số dư một loại tiền tệ trong <see cref="Wallet"/> (một dòng <c>currency → amount</c>). Hỗ trợ cộng
/// (credit) và trừ (spend); bất biến <b>số nguyên không âm</b> (ADR-011) được bảo vệ ở
/// <see cref="Subtract"/>.
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
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Delta cộng không được âm.");
        }

        Amount += delta;
    }

    /// <summary>
    /// Trừ khỏi số dư (chỉ dùng nội bộ <see cref="Wallet.Spend"/>). Bất biến không âm: ném nếu kết quả &lt; 0.
    /// </summary>
    internal void Subtract(long delta)
    {
        if (delta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Delta trừ không được âm.");
        }

        if (Amount - delta < 0)
        {
            throw new InvalidOperationException("Số dư không được âm (bất biến ADR-011).");
        }

        Amount -= delta;
    }
}
