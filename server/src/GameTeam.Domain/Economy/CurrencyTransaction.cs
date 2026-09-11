using GameTeam.Domain.Common;

namespace GameTeam.Domain.Economy;

/// <summary>
/// Một dòng sổ cái (ledger) giao dịch tiền tệ — <b>append-only</b>, server-authoritative (ADR-007). Vừa là
/// <b>audit log</b> (ai/loại tiền/biến động/nguồn-sink/thời điểm), vừa là <b>bản ghi idempotency</b>:
/// <see cref="IdempotencyKey"/> là <b>duy nhất</b> (unique index) nên gọi lại cùng key trả lại đúng kết quả
/// đã lưu mà KHÔNG áp dụng lần hai (chống double-grant/double-spend). Tổng quát hoá mẫu Phase 30
/// (<c>BattleRecord</c>) thành cơ chế tái dùng cho mọi nguồn/sink (gacha 33, AFK 37, shop 40, mail 42).
/// </summary>
public sealed class CurrencyTransaction : AggregateRoot<Guid>
{
    /// <summary>Phiên bản schema của bản ghi (ADR-007). Tăng kèm migration + test khi cấu trúc đổi.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private CurrencyTransaction()
    {
    }

    private CurrencyTransaction(
        Guid id,
        Guid profileId,
        string currency,
        long delta,
        long balanceAfter,
        string source,
        string idempotencyKey,
        int schemaVersion,
        DateTimeOffset createdAt)
        : base(id)
    {
        ProfileId = profileId;
        Currency = currency;
        Delta = delta;
        BalanceAfter = balanceAfter;
        Source = source;
        IdempotencyKey = idempotencyKey;
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
    }

    /// <summary>Profile sở hữu (khoá ngoại tới <c>player_profiles</c>). Server-controlled.</summary>
    public Guid ProfileId { get; private set; }

    /// <summary>Mã tiền tệ (<c>gold</c>/<c>gem</c>/<c>ticket</c>).</summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>Biến động số dư có dấu: <c>&gt; 0</c> là cấp (grant), <c>&lt; 0</c> là tiêu (spend).</summary>
    public long Delta { get; private set; }

    /// <summary>Số dư sau giao dịch (snapshot để trả lại kết quả khi retry idempotent).</summary>
    public long BalanceAfter { get; private set; }

    /// <summary>Nguồn/sink của giao dịch (lý do — ví dụ <c>battle_reward</c>, <c>gacha_summon</c>). Cho audit/vận hành.</summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>Khoá idempotency (duy nhất) — chống thực hiện lại cùng một thao tác. Server-controlled.</summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>Phiên bản schema của bản ghi này (ADR-007). Server-controlled.</summary>
    public int SchemaVersion { get; private set; } = CurrentSchemaVersion;

    /// <summary>Thời điểm ghi (server-time, từ <see cref="IClock"/>).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary><c>true</c> nếu là giao dịch cấp (credit); <c>false</c> nếu là tiêu (spend).</summary>
    public bool IsGrant => Delta > 0;

    /// <summary>
    /// Ghi một dòng ledger mới. <paramref name="id"/> do caller sinh (<c>Guid.NewGuid()</c>).
    /// <paramref name="delta"/> phải khác 0 (dương = grant, âm = spend). Guard tham số. Không raise event.
    /// </summary>
    public static CurrencyTransaction Record(
        Guid id,
        Guid profileId,
        string currency,
        long delta,
        long balanceAfter,
        string source,
        string idempotencyKey,
        DateTimeOffset nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("CurrencyTransaction id không được rỗng.", nameof(id));
        }

        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("ProfileId không được rỗng.", nameof(profileId));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency không được rỗng.", nameof(currency));
        }

        if (delta == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Delta phải khác 0 (grant dương / spend âm).");
        }

        if (balanceAfter < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(balanceAfter), balanceAfter, "Số dư sau giao dịch không được âm.");
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source không được rỗng.", nameof(source));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("IdempotencyKey không được rỗng.", nameof(idempotencyKey));
        }

        return new CurrencyTransaction(
            id, profileId, currency, delta, balanceAfter, source, idempotencyKey, CurrentSchemaVersion, nowUtc);
    }

    /// <summary>Dựng lại từ trạng thái đã lưu — hydration/thử nghiệm.</summary>
    public static CurrencyTransaction Restore(
        Guid id,
        Guid profileId,
        string currency,
        long delta,
        long balanceAfter,
        string source,
        string idempotencyKey,
        int schemaVersion,
        DateTimeOffset createdAt)
        => new(id, profileId, currency, delta, balanceAfter, source, idempotencyKey, schemaVersion, createdAt);
}
