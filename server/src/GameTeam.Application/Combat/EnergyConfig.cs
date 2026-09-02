namespace GameTeam.Application.Combat;

/// <summary>Config năng lượng (§15, CB4 [ĐỀ XUẤT]). Vector mẫu đặt mọi giá trị gain = 0 (tắt).</summary>
public sealed class EnergyConfig
{
    /// <summary>Năng lượng ban đầu.</summary>
    public int Initial { get; init; }

    /// <summary>Cộng khi ra đòn thường.</summary>
    public int OnAttack { get; init; }

    /// <summary>Cộng khi trúng đòn (còn sống).</summary>
    public int OnHit { get; init; }

    /// <summary>Chi phí ultimate.</summary>
    public int UltimateCost { get; init; }

    /// <summary>Trần năng lượng.</summary>
    public int Max { get; init; }
}
