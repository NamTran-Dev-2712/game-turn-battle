using GameTeam.Domain.Common;

namespace GameTeam.Domain.Profiles;

/// <summary>
/// Hồ sơ người chơi — <b>gốc save server-authoritative</b> (ADR-007): chân lý ở PostgreSQL, client chỉ cache đọc.
/// Gắn 1-1 với một <see cref="Accounts.Account"/> qua <see cref="AccountId"/> (unique). Là "gốc" mà mọi state
/// feature về sau (currency 31, hero 27/35, inventory 32, progress 34) <b>mở rộng</b> — thêm bảng/tham chiếu +
/// tăng <see cref="SchemaVersion"/> khi cấu trúc đổi.
/// <para>
/// <b>Versioning + migration (ADR-007):</b> <see cref="SchemaVersion"/> là phiên bản schema của bản ghi; khi đọc
/// một profile cũ hơn <see cref="CurrentSchemaVersion"/>, <see cref="Upgrade"/> nâng cấp dữ liệu theo chuỗi
/// <c>v(N) → v(N+1)</c> mà KHÔNG mất dữ liệu (read-repair). Đây là migration <b>dữ liệu profile</b> — khác với
/// EF Core DDL migration (tạo bảng, ở Infrastructure).
/// </para>
/// <para>
/// <b>Server-authoritative:</b> mọi thay đổi qua command server; client KHÔNG đặt <see cref="SchemaVersion"/>,
/// <see cref="AccountId"/>, chủ sở hữu hay timestamp. Phase 19 KHÔNG chứa state nghiệp vụ cụ thể — chỉ khung nền.
/// </para>
/// </summary>
public sealed class PlayerProfile : AggregateRoot<Guid>
{
    /// <summary>Phiên bản schema profile hiện hành. Tăng khi cấu trúc profile đổi (kèm migration + test + doc).</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Tên hiển thị mặc định cho profile mới (chưa có tên do người chơi đặt).</summary>
    public const string DefaultDisplayName = "Guest";

    /// <summary>Cấp khởi tạo của một profile mới.</summary>
    public const int InitialLevel = 1;

    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private PlayerProfile()
    {
    }

    private PlayerProfile(
        Guid id,
        Guid accountId,
        string displayName,
        int level,
        int schemaVersion,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id)
    {
        AccountId = accountId;
        DisplayName = displayName;
        Level = level;
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Tài khoản sở hữu (quan hệ 1-1; unique ở DB). Server-controlled — không nhận từ client.</summary>
    public Guid AccountId { get; private set; }

    /// <summary>Tên hiển thị (bản nền — Phase 05 contract).</summary>
    public string DisplayName { get; private set; } = DefaultDisplayName;

    /// <summary>Cấp tài khoản người chơi (bản nền — Phase 05 contract).</summary>
    public int Level { get; private set; } = InitialLevel;

    /// <summary>Phiên bản schema của bản ghi này (ADR-007). Server-controlled.</summary>
    public int SchemaVersion { get; private set; } = CurrentSchemaVersion;

    /// <summary>Thời điểm tạo (server-time, từ <see cref="IClock"/>).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Thời điểm cập nhật gần nhất (server-time). Tăng khi migrate/đổi state.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Tạo profile mới cho một tài khoản (guest login lần đầu). <paramref name="id"/> do caller sinh
    /// (<c>Guid.NewGuid()</c>). Đặt <see cref="SchemaVersion"/> = <see cref="CurrentSchemaVersion"/>, stamp
    /// timestamp bằng server-time. Raise <see cref="PlayerProfileCreated"/>.
    /// </summary>
    /// <param name="id">Định danh profile mới (không rỗng).</param>
    /// <param name="accountId">Tài khoản sở hữu (không rỗng).</param>
    /// <param name="nowUtc">Server-time (từ <see cref="IClock"/>).</param>
    public static PlayerProfile CreateForAccount(Guid id, Guid accountId, DateTimeOffset nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("PlayerProfile id không được rỗng.", nameof(id));
        }

        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("AccountId không được rỗng.", nameof(accountId));
        }

        PlayerProfile profile = new(
            id,
            accountId,
            DefaultDisplayName,
            InitialLevel,
            CurrentSchemaVersion,
            nowUtc,
            nowUtc);

        profile.RaiseDomainEvent(new PlayerProfileCreated(id, accountId));
        return profile;
    }

    /// <summary>
    /// Dựng lại profile từ trạng thái đã lưu (bất kỳ phiên bản schema nào) — KHÔNG raise event. Dùng cho
    /// việc phục dựng/thử nghiệm migration; không phải luồng tạo mới. Không nâng cấp version tại đây — gọi
    /// <see cref="Upgrade"/> tường minh.
    /// </summary>
    public static PlayerProfile Restore(
        Guid id,
        Guid accountId,
        string displayName,
        int level,
        int schemaVersion,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => new(id, accountId, displayName, level, schemaVersion, createdAt, updatedAt);

    /// <summary>
    /// Nâng cấp dữ liệu profile lên <see cref="CurrentSchemaVersion"/> theo chuỗi <c>v(N) → v(N+1)</c>
    /// (read-repair, ADR-007). Trả <c>true</c> nếu có nâng cấp (đã đổi <see cref="SchemaVersion"/> ⇒ cần persist).
    /// Deterministic, không I/O. Mỗi lần bump schema, thêm một bước <c>MigrateV{n}ToV{n+1}</c> ở đây.
    /// </summary>
    /// <param name="nowUtc">Server-time để stamp <see cref="UpdatedAt"/> khi có thay đổi.</param>
    public bool Upgrade(DateTimeOffset nowUtc)
    {
        if (SchemaVersion >= CurrentSchemaVersion)
        {
            return false;
        }

        while (SchemaVersion < CurrentSchemaVersion)
        {
            switch (SchemaVersion)
            {
                case 0:
                    MigrateV0ToV1();
                    break;
                default:
                    // Không có đường nâng cấp cho version này ⇒ lỗi lập trình (thiếu bước migration).
                    throw new InvalidOperationException(
                        $"Thiếu bước migration cho PlayerProfile schema version {SchemaVersion}.");
            }

            SchemaVersion++;
        }

        UpdatedAt = nowUtc;
        return true;
    }

    /// <summary>
    /// Bước migration mẫu v0 → v1: profile v0 (legacy) có thể thiếu <see cref="DisplayName"/> ⇒ back-fill
    /// tên mặc định, <b>bảo toàn</b> các trường còn lại (vd <see cref="Level"/> — nhân chứng preservation).
    /// </summary>
    private void MigrateV0ToV1()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            DisplayName = DefaultDisplayName;
        }
    }
}
