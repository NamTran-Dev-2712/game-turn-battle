using GameTeam.Domain.Common;

namespace GameTeam.Domain.Inventory;

/// <summary>
/// Kho đồ (inventory) của một người chơi — gắn 1-1 với gốc save <see cref="Profiles.PlayerProfile"/> qua
/// <see cref="ProfileId"/> (unique). Server-authoritative (ADR-007), là "sổ tài sản" thứ hai bên cạnh ví
/// (<see cref="Economy.Wallet"/>). <b>Phase 32</b>: chứa các chồng vật phẩm/mảnh (<see cref="ItemStack"/>) với
/// bất biến <b>số lượng không âm</b>; thao tác thêm/bớt <b>nhiều item atomic</b> + idempotency + audit ledger
/// nằm ở tầng Application (<c>InventoryService</c> + <see cref="InventoryTransaction"/>). Hero sở hữu KHÔNG
/// lưu ở đây — hero là aggregate riêng <see cref="Heroes.OwnedHero"/> (Phase 27), inventory <b>chiếu</b> khi
/// truy vấn (không nhân bản). Số lượng số nguyên (ADR-011).
/// </summary>
public sealed class Inventory : AggregateRoot<Guid>
{
    /// <summary>Phiên bản schema của bản ghi (ADR-007). Tăng kèm migration + test khi cấu trúc đổi.</summary>
    public const int CurrentSchemaVersion = 1;

    private readonly List<ItemStack> _stacks = [];

    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private Inventory()
    {
    }

    private Inventory(
        Guid id,
        Guid profileId,
        IEnumerable<ItemStack> stacks,
        int schemaVersion,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id)
    {
        ProfileId = profileId;
        _stacks = stacks.ToList();
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

    /// <summary>Các chồng tài sản (chỉ đọc).</summary>
    public IReadOnlyList<ItemStack> Stacks => _stacks.AsReadOnly();

    /// <summary>Tạo kho rỗng cho một profile. <paramref name="id"/> do caller sinh. Guard tham số.</summary>
    public static Inventory CreateFor(Guid id, Guid profileId, DateTimeOffset nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Inventory id không được rỗng.", nameof(id));
        }

        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("ProfileId không được rỗng.", nameof(profileId));
        }

        return new Inventory(id, profileId, [], CurrentSchemaVersion, nowUtc, nowUtc);
    }

    /// <summary>Dựng lại từ trạng thái đã lưu — hydration.</summary>
    public static Inventory Restore(
        Guid id,
        Guid profileId,
        IReadOnlyList<ItemStack> stacks,
        int schemaVersion,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => new(id, profileId, stacks, schemaVersion, createdAt, updatedAt);

    /// <summary>
    /// Cộng <paramref name="quantity"/> (&gt; 0) vào chồng <c>(itemType, itemId)</c> (tạo dòng nếu chưa có) và
    /// cập nhật <see cref="UpdatedAt"/>. Trả về số lượng sau khi cộng.
    /// </summary>
    public long Add(string itemType, string itemId, long quantity, DateTimeOffset nowUtc)
    {
        GuardTypeId(itemType, itemId);
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity thêm phải dương.");
        }

        ItemStack? existing = Find(itemType, itemId);
        if (existing is null)
        {
            existing = new ItemStack(itemType, itemId, quantity);
            _stacks.Add(existing);
        }
        else
        {
            existing.Add(quantity);
        }

        UpdatedAt = nowUtc;
        return existing.Quantity;
    }

    /// <summary>
    /// Trừ <paramref name="quantity"/> (&gt; 0) khỏi chồng <c>(itemType, itemId)</c> và cập nhật
    /// <see cref="UpdatedAt"/>. Trả về số lượng sau khi trừ. Bất biến <b>số lượng không âm</b> được bảo vệ ở
    /// đây (backstop lỗi lập trình): người gọi đã kiểm đủ số lượng bằng <see cref="QuantityOf"/> và trả
    /// <c>Result</c> lỗi nghiệp vụ trước khi tới đây; nếu vẫn thiếu ⇒ ném (không phải luồng nghiệp vụ).
    /// </summary>
    public long Remove(string itemType, string itemId, long quantity, DateTimeOffset nowUtc)
    {
        GuardTypeId(itemType, itemId);
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity bớt phải dương.");
        }

        ItemStack? existing = Find(itemType, itemId);
        if (existing is null || existing.Quantity < quantity)
        {
            throw new InvalidOperationException(
                $"Số lượng '{itemType}:{itemId}' không đủ để bớt {quantity} (bất biến không âm) — người gọi phải kiểm QuantityOf trước.");
        }

        existing.Subtract(quantity);
        UpdatedAt = nowUtc;
        return existing.Quantity;
    }

    /// <summary>Số lượng hiện tại của một chồng (0 nếu chưa có dòng).</summary>
    public long QuantityOf(string itemType, string itemId) => Find(itemType, itemId)?.Quantity ?? 0;

    private ItemStack? Find(string itemType, string itemId) => _stacks.FirstOrDefault(s =>
        string.Equals(s.ItemType, itemType, StringComparison.Ordinal) &&
        string.Equals(s.ItemId, itemId, StringComparison.Ordinal));

    private static void GuardTypeId(string itemType, string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemType))
        {
            throw new ArgumentException("ItemType không được rỗng.", nameof(itemType));
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("ItemId không được rỗng.", nameof(itemId));
        }
    }
}
