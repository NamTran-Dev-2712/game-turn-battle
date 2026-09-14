using GameTeam.Application.Combat;
using GameTeam.Domain.Common;
using DomainTeam = GameTeam.Domain.Teams.Team;

namespace GameTeam.Application.Features.Teams;

/// <summary>
/// Dựng <b>team snapshot</b> bất biến từ đội hình đã lưu để đưa vào combat sim (feed <c>BattleRequest.Ally</c>
/// → <see cref="CombatInputResolver"/>, phase 30). Snapshot là danh sách <see cref="CombatTeamMember"/> — bản
/// ghi giá trị (immutable record), tách khỏi aggregate <see cref="DomainTeam"/> đang mutable: sim KHÔNG giữ
/// tham chiếu profile/team mutable (ADR-011). Chỉ số hero lấy từ config <c>config@vN</c> bất biến ở
/// <c>CombatInputResolver</c> ⇒ "chỉ số tại thời điểm" ổn định theo config version.
/// <para>
/// <see cref="TeamSlot.SlotIndex"/> đi thẳng vào <c>slot</c> combat ⇒ vị trí formation ảnh hưởng
/// target/aggro (combat-framework §14). <c>actor_id</c> ổn định = <c>ally_{slotIndex}</c> (định danh cuối
/// cho mọi tie-break, §13/§14).
/// </para>
/// </summary>
public static class TeamSnapshotFactory
{
    /// <summary>Tiền tố định danh đơn vị đội ally trong trận.</summary>
    public const string AllyActorPrefix = "ally_";

    /// <summary>
    /// Ánh xạ đội hình đã lưu → danh sách <see cref="CombatTeamMember"/> bất biến (sắp theo slot). Kết quả là
    /// bản sao giá trị: thay đổi <paramref name="team"/> sau đó KHÔNG ảnh hưởng snapshot đã tạo.
    /// <para>
    /// <paramref name="levelByHeroId"/> (Phase 35): cấp hero owned theo <c>hero_id</c> — chỉ số vào trận tính
    /// theo cấp này ở <see cref="CombatInputResolver"/> ⇒ nâng cấp hero ảnh hưởng combat. Thiếu (null/không có
    /// mục) ⇒ cấp 1 (chỉ số nền) để tương thích ngược. Cấp là "chỉ số tại thời điểm" trong snapshot bất biến.
    /// </para>
    /// </summary>
    public static IReadOnlyList<CombatTeamMember> Create(
        DomainTeam team, IReadOnlyDictionary<string, int>? levelByHeroId = null)
    {
        Guard.NotNull(team);
        return team.Slots
            .OrderBy(s => s.SlotIndex)
            .Select(s => new CombatTeamMember(
                $"{AllyActorPrefix}{s.SlotIndex}",
                s.HeroId,
                s.SlotIndex,
                LevelOf(levelByHeroId, s.HeroId)))
            .ToList();
    }

    private static int LevelOf(IReadOnlyDictionary<string, int>? levelByHeroId, string heroId)
        => levelByHeroId is not null && levelByHeroId.TryGetValue(heroId, out int level) && level >= 1
            ? level
            : 1;
}
