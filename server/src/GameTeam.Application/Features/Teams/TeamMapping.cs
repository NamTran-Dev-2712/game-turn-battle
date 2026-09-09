using GameTeam.Contracts.Team;
using DomainTeam = GameTeam.Domain.Teams.Team;

namespace GameTeam.Application.Features.Teams;

/// <summary>
/// Map aggregate <see cref="DomainTeam"/> ↔ DTO wire (phase 05). EF entity KHÔNG trả thẳng ra API — chỉ
/// projection read-model. Ô sắp theo <c>SlotIndex</c> để wire ổn định.
/// </summary>
internal static class TeamMapping
{
    /// <summary>Khoá loại config của formation trong bundle (data-driven — dùng cho <c>IConfigProvider</c>).</summary>
    public const string FormationConfigType = "formation";

    /// <summary>Id cấu hình formation mặc định (MVP: một lưới dùng chung).</summary>
    public const string DefaultFormationId = "formation_default";

    public static TeamDto ToDto(DomainTeam team)
        => new(
            team.Id,
            team.Slots
                .OrderBy(s => s.SlotIndex)
                .Select(s => new TeamSlotDto(s.SlotIndex, s.HeroId))
                .ToList());

    /// <summary>DTO đội rỗng (chưa lưu đội nào) — client dựng lưới trống (Id = <see cref="Guid.Empty"/>).</summary>
    public static TeamDto Empty() => new(Guid.Empty, Array.Empty<TeamSlotDto>());
}
