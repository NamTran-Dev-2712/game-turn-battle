namespace GameTeam.Application.Combat;

/// <summary>
/// Một đơn vị địch trong stage config (bám <c>stage.schema.json</c> enemies): <c>hero_id</c> (đọc chỉ số từ
/// hero config) + <c>slot</c> tuỳ chọn (vị trí đội hình; vắng ⇒ suy theo thứ tự). Định danh trong trận
/// (<c>actor_id</c>) do <see cref="CombatInputResolver"/> suy ra (<c>enemy_{i}</c>) — ổn định, tất định.
/// </summary>
public sealed class StageEnemyConfig
{
    /// <summary>Hero tham chiếu (để đọc chỉ số).</summary>
    public string HeroId { get; init; } = string.Empty;

    /// <summary>Vị trí đội hình (tuỳ chọn; vắng ⇒ suy theo chỉ số thứ tự trong danh sách).</summary>
    public int? Slot { get; init; }
}
