using GameTeam.Domain.Common;

namespace GameTeam.Domain.Accounts;

/// <summary>
/// Tài khoản người chơi — <b>ranh giới định danh</b> của toàn bộ trạng thái server-authoritative (ADR-007).
/// Là điểm vào xác thực: guest login tạo một <see cref="Account"/> rồi cấp JWT mang <c>sub = Id</c>.
/// <para>
/// <b>Chừa chỗ liên kết provider (Post-MVP):</b> việc gắn Google/Apple/email sẽ là một bảng riêng
/// <c>account_providers</c> tham chiếu <see cref="Id"/> (quan hệ 1-N), KHÔNG thêm cột provider vào đây
/// (YAGNI). Nhờ vậy schema mở rộng được mà không phá <see cref="Account"/> (ADR-006).
/// </para>
/// </summary>
public sealed class Account : AggregateRoot<Guid>
{
    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private Account()
    {
    }

    private Account(Guid id, AccountType type, DateTimeOffset createdAt)
        : base(id)
    {
        Type = type;
        CreatedAt = createdAt;
    }

    /// <summary>Loại tài khoản (MVP: <see cref="AccountType.Guest"/>).</summary>
    public AccountType Type { get; private set; }

    /// <summary>Thời điểm tạo (server-time, lấy từ <see cref="IClock"/> — không dùng wall-clock).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Tạo tài khoản khách mới. <paramref name="id"/> do caller sinh (<c>Guid.NewGuid()</c>) để biết
    /// <c>sub</c> của JWT TRƯỚC khi persist (không cần round-trip DB). Raise <see cref="AccountCreated"/>.
    /// </summary>
    /// <param name="id">Định danh mới (không rỗng).</param>
    /// <param name="createdAtUtc">Thời điểm tạo (server-time).</param>
    public static Account CreateGuest(Guid id, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Account id không được rỗng.", nameof(id));
        }

        Account account = new(id, AccountType.Guest, createdAtUtc);
        account.RaiseDomainEvent(new AccountCreated(id, AccountType.Guest));
        return account;
    }
}
