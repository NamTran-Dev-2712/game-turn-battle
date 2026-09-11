using GameTeam.Domain.Common;

namespace GameTeam.Domain.Economy;

/// <summary>
/// Ví tiền tệ của một người chơi — gắn 1-1 với gốc save <see cref="Profiles.PlayerProfile"/> qua
/// <see cref="ProfileId"/> (unique). Server-authoritative (ADR-007). <b>Phase 31</b>: hỗ trợ
/// <b>cấp (credit)</b> và <b>tiêu (spend)</b> có bất biến <b>số dư không âm</b>; giao dịch atomic +
/// idempotency + audit ledger nằm ở tầng Application (<c>CurrencyWalletService</c> + <c>CurrencyTransaction</c>).
/// Loại tiền là mã chuỗi (<c>gold</c>/<c>gem</c>/<c>ticket</c>) — ánh xạ <see cref="GameTeam.Contracts"/>
/// <c>Currency</c> ở ranh giới Application. Số dư số nguyên (ADR-011).
/// </summary>
public sealed class Wallet : AggregateRoot<Guid>
{
    /// <summary>Phiên bản schema của bản ghi (ADR-007). Tăng kèm migration + test khi cấu trúc đổi.</summary>
    public const int CurrentSchemaVersion = 1;

    private readonly List<WalletBalance> _balances = [];

    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private Wallet()
    {
    }

    private Wallet(
        Guid id,
        Guid profileId,
        IEnumerable<WalletBalance> balances,
        int schemaVersion,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id)
    {
        ProfileId = profileId;
        _balances = balances.ToList();
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Profile sở hữu (khoá ngoại tới <c>player_profiles</c>, unique). Server-controlled.</summary>
    public Guid ProfileId { get; private set; }

    /// <summary>Phiên bản schema của bản ghi này (ADR-007). Server-controlled.</summary>
    public int SchemaVersion { get; private set; } = CurrentSchemaVersion;

    /// <summary>Thời điểm tạo (server-time, từ <see cref="IClock"/>).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Thời điểm cập nhật gần nhất (server-time).</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Các dòng số dư (chỉ đọc).</summary>
    public IReadOnlyList<WalletBalance> Balances => _balances.AsReadOnly();

    /// <summary>Tạo ví rỗng cho một profile. <paramref name="id"/> do caller sinh. Guard tham số.</summary>
    public static Wallet CreateFor(Guid id, Guid profileId, DateTimeOffset nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Wallet id không được rỗng.", nameof(id));
        }

        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("ProfileId không được rỗng.", nameof(profileId));
        }

        return new Wallet(id, profileId, [], CurrentSchemaVersion, nowUtc, nowUtc);
    }

    /// <summary>Dựng lại từ trạng thái đã lưu — hydration.</summary>
    public static Wallet Restore(
        Guid id,
        Guid profileId,
        IReadOnlyList<WalletBalance> balances,
        int schemaVersion,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => new(id, profileId, balances, schemaVersion, createdAt, updatedAt);

    /// <summary>
    /// Cộng <paramref name="amount"/> (&gt; 0) vào số dư <paramref name="currency"/> (tạo dòng nếu chưa có) và
    /// cập nhật <see cref="UpdatedAt"/>. Trả về số dư sau khi cộng.
    /// </summary>
    public long Credit(string currency, long amount, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency không được rỗng.", nameof(currency));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount cấp phải dương.");
        }

        WalletBalance? existing = _balances.FirstOrDefault(b => string.Equals(b.Currency, currency, StringComparison.Ordinal));
        if (existing is null)
        {
            existing = new WalletBalance(currency, amount);
            _balances.Add(existing);
        }
        else
        {
            existing.Add(amount);
        }

        UpdatedAt = nowUtc;
        return existing.Amount;
    }

    /// <summary>
    /// Trừ <paramref name="amount"/> (&gt; 0) khỏi số dư <paramref name="currency"/> và cập nhật
    /// <see cref="UpdatedAt"/>. Trả về số dư sau khi trừ. Bất biến <b>số dư không âm</b> được bảo vệ ở đây
    /// (backstop lỗi lập trình): người gọi đã kiểm đủ tiền bằng <see cref="BalanceOf"/> và trả
    /// <c>Result</c> lỗi nghiệp vụ trước khi tới đây; nếu vẫn thiếu ⇒ ném (không phải luồng nghiệp vụ).
    /// </summary>
    public long Spend(string currency, long amount, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency không được rỗng.", nameof(currency));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount tiêu phải dương.");
        }

        WalletBalance? existing = _balances.FirstOrDefault(b => string.Equals(b.Currency, currency, StringComparison.Ordinal));
        if (existing is null || existing.Amount < amount)
        {
            throw new InvalidOperationException(
                $"Số dư '{currency}' không đủ để tiêu {amount} (bất biến không âm) — người gọi phải kiểm BalanceOf trước.");
        }

        existing.Subtract(amount);
        UpdatedAt = nowUtc;
        return existing.Amount;
    }

    /// <summary>Số dư hiện tại của một loại tiền tệ (0 nếu chưa có dòng).</summary>
    public long BalanceOf(string currency) =>
        _balances.FirstOrDefault(b => string.Equals(b.Currency, currency, StringComparison.Ordinal))?.Amount ?? 0;
}
