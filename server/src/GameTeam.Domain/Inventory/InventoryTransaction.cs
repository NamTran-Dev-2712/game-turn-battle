using GameTeam.Domain.Common;

namespace GameTeam.Domain.Inventory;

/// <summary>
/// Một bản ghi sổ cái (ledger) giao dịch kho đồ — <b>append-only</b>, server-authoritative (ADR-007). Vừa là
/// <b>audit log</b> (ai/nguồn-sink/biến động item/thời điểm), vừa là <b>bản ghi idempotency</b>:
/// <see cref="IdempotencyKey"/> là <b>duy nhất</b> (unique index) nên gọi lại cùng key trả lại đúng kết quả
/// đã lưu mà KHÔNG áp dụng lần hai (chống double-add/double-remove). Tổng quát hoá mẫu ledger Phase 31
/// (<c>CurrencyTransaction</c>) cho thao tác <b>nhiều-item atomic</b>: một transaction = một command, chứa
/// nhiều dòng <see cref="InventoryChange"/>. Cơ chế tái dùng cho mọi nguồn/sink (gacha 33, shop 40, mail 42).
/// </summary>
public sealed class InventoryTransaction : AggregateRoot<Guid>
{
    /// <summary>Chiều grant (thêm item).</summary>
    public const string GrantDirection = "grant";

    /// <summary>Chiều consume (bớt item).</summary>
    public const string ConsumeDirection = "consume";

    /// <summary>Phiên bản schema của bản ghi (ADR-007). Tăng kèm migration + test khi cấu trúc đổi.</summary>
    public const int CurrentSchemaVersion = 1;

    private readonly List<InventoryChange> _changes = [];

    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private InventoryTransaction()
    {
    }

    private InventoryTransaction(
        Guid id,
        Guid profileId,
        string direction,
        IEnumerable<InventoryChange> changes,
        string source,
        string idempotencyKey,
        int schemaVersion,
        DateTimeOffset createdAt)
        : base(id)
    {
        ProfileId = profileId;
        Direction = direction;
        _changes = changes.ToList();
        Source = source;
        IdempotencyKey = idempotencyKey;
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
    }

    /// <summary>Profile sở hữu (khoá ngoại tới <c>player_profiles</c>). Server-controlled.</summary>
    public Guid ProfileId { get; private set; }

    /// <summary>Chiều giao dịch (<c>grant</c>/<c>consume</c>) — cho audit/vận hành.</summary>
    public string Direction { get; private set; } = string.Empty;

    /// <summary>Nguồn/sink của giao dịch (lý do — ví dụ <c>gacha_summon</c>, <c>seed_starter</c>). Cho audit.</summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>Khoá idempotency (duy nhất) — chống thực hiện lại cùng một thao tác. Server-controlled.</summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>Phiên bản schema của bản ghi này (ADR-007). Server-controlled.</summary>
    public int SchemaVersion { get; private set; } = CurrentSchemaVersion;

    /// <summary>Thời điểm ghi (server-time, từ <see cref="IClock"/>).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Các dòng thay đổi item (chỉ đọc).</summary>
    public IReadOnlyList<InventoryChange> Changes => _changes.AsReadOnly();

    /// <summary><c>true</c> nếu là giao dịch grant; <c>false</c> nếu consume.</summary>
    public bool IsGrant => string.Equals(Direction, GrantDirection, StringComparison.Ordinal);

    /// <summary>
    /// Ghi một dòng ledger mới. <paramref name="id"/> do caller sinh (<c>Guid.NewGuid()</c>).
    /// <paramref name="changes"/> phải khác rỗng. Guard tham số. Không raise event.
    /// </summary>
    public static InventoryTransaction Record(
        Guid id,
        Guid profileId,
        string direction,
        IReadOnlyList<InventoryChange> changes,
        string source,
        string idempotencyKey,
        DateTimeOffset nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("InventoryTransaction id không được rỗng.", nameof(id));
        }

        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("ProfileId không được rỗng.", nameof(profileId));
        }

        if (direction is not (GrantDirection or ConsumeDirection))
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Direction phải là 'grant' hoặc 'consume'.");
        }

        if (changes is null || changes.Count == 0)
        {
            throw new ArgumentException("Changes không được rỗng.", nameof(changes));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source không được rỗng.", nameof(source));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("IdempotencyKey không được rỗng.", nameof(idempotencyKey));
        }

        return new InventoryTransaction(
            id, profileId, direction, changes, source, idempotencyKey, CurrentSchemaVersion, nowUtc);
    }

    /// <summary>Dựng lại từ trạng thái đã lưu — hydration/thử nghiệm.</summary>
    public static InventoryTransaction Restore(
        Guid id,
        Guid profileId,
        string direction,
        IReadOnlyList<InventoryChange> changes,
        string source,
        string idempotencyKey,
        int schemaVersion,
        DateTimeOffset createdAt)
        => new(id, profileId, direction, changes, source, idempotencyKey, schemaVersion, createdAt);
}
