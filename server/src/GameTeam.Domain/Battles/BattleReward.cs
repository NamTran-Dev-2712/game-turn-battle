namespace GameTeam.Domain.Battles;

/// <summary>
/// Một khoản thưởng đã cấp trong một trận (bản ghi bất biến của một entry reward table — <c>reward.schema.json</c>).
/// Lưu kèm <see cref="BattleRecord"/> để trả lại nguyên vẹn khi retry idempotent (không tính lại). Số là integer
/// (ADR-011).
/// </summary>
public sealed class BattleReward
{
    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private BattleReward()
    {
    }

    /// <summary>Dựng một khoản thưởng đã cấp. Guard: loại/ref không rỗng, số lượng &gt; 0.</summary>
    public BattleReward(string rewardType, string refId, int amount)
    {
        if (string.IsNullOrWhiteSpace(rewardType))
        {
            throw new ArgumentException("RewardType không được rỗng.", nameof(rewardType));
        }

        if (string.IsNullOrWhiteSpace(refId))
        {
            throw new ArgumentException("RefId không được rỗng.", nameof(refId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount phải dương.");
        }

        RewardType = rewardType;
        RefId = refId;
        Amount = amount;
    }

    /// <summary>Loại thưởng (<c>currency</c>/<c>hero</c>/<c>fragment</c>/<c>item</c>).</summary>
    public string RewardType { get; private set; } = string.Empty;

    /// <summary>Khoá tham chiếu thực thể thưởng (vd <c>gold</c>).</summary>
    public string RefId { get; private set; } = string.Empty;

    /// <summary>Số lượng (integer &gt; 0).</summary>
    public int Amount { get; private set; }
}
