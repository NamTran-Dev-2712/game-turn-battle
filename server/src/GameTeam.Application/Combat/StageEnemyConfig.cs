namespace GameTeam.Application.Combat;

/// <summary>Một đơn vị địch trong stage config (định danh + hero + slot; chỉ số lấy từ hero config).</summary>
public sealed class StageEnemyConfig
{
    /// <summary>Định danh ổn định trong trận.</summary>
    public string ActorId { get; init; } = string.Empty;

    /// <summary>Hero tham chiếu (để đọc chỉ số).</summary>
    public string HeroId { get; init; } = string.Empty;

    /// <summary>Vị trí đội hình.</summary>
    public int Slot { get; init; }
}
