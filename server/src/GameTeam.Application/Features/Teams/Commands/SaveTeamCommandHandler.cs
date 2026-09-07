using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Contracts.Team;
using GameTeam.Domain.Common;
using GameTeam.Domain.Heroes;
using GameTeam.Domain.Teams;
using MediatR;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;
using DomainTeam = GameTeam.Domain.Teams.Team;

namespace GameTeam.Application.Features.Teams.Commands;

/// <summary>
/// Handles <see cref="SaveTeamCommand"/>: chủ sở hữu suy từ <see cref="ICurrentUser"/> (chống IDOR) → profile →
/// đọc lưới từ config → validate <b>server-authoritative</b> (đúng số ô, slot trong lưới không trùng, hero
/// không trùng, hero thuộc sở hữu) → upsert đội (tạo mới hoặc <c>Replace</c>). Ghi commit nguyên tử bởi
/// <c>TransactionBehavior</c>. Client chỉ gửi intent — mọi quyết định ở đây (ADR-007/011).
/// </summary>
public sealed class SaveTeamCommandHandler : IRequestHandler<SaveTeamCommand, Result<TeamDto>>
{
    private readonly ITeamRepository _teams;
    private readonly IOwnedHeroRepository _ownedHeroes;
    private readonly IPlayerProfileRepository _profiles;
    private readonly IConfigProvider _config;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public SaveTeamCommandHandler(
        ITeamRepository teams,
        IOwnedHeroRepository ownedHeroes,
        IPlayerProfileRepository profiles,
        IConfigProvider config,
        ICurrentUser currentUser,
        IClock clock)
    {
        _teams = teams;
        _ownedHeroes = ownedHeroes;
        _profiles = profiles;
        _config = config;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<TeamDto>> Handle(SaveTeamCommand request, CancellationToken cancellationToken)
    {
        Guid? accountId = _currentUser.AccountId;
        if (accountId is null)
        {
            return TeamErrors.Unauthenticated;
        }

        DomainProfile? profile = await _profiles.GetByAccountIdAsync(accountId.Value, cancellationToken);
        if (profile is null)
        {
            return TeamErrors.ProfileNotFound;
        }

        FormationConfig? formation = _config.Get<FormationConfig>(
            TeamMapping.FormationConfigType, TeamMapping.DefaultFormationId);
        if (formation is null || formation.SlotCount <= 0)
        {
            return TeamErrors.FormationConfigMissing;
        }

        int slotCount = formation.SlotCount;
        IReadOnlyList<TeamSlotDto> slots = request.Slots;

        // Rule 1 — đúng số hero (đúng số ô lưới).
        if (slots.Count != slotCount)
        {
            return TeamErrors.InvalidSize;
        }

        // Rule 4 — slot hợp lệ: trong [0, slotCount), không trùng vị trí.
        if (slots.Any(s => s.SlotIndex < 0 || s.SlotIndex >= slotCount)
            || slots.Select(s => s.SlotIndex).Distinct().Count() != slots.Count)
        {
            return TeamErrors.InvalidSlot;
        }

        // Rule 3 — không trùng hero.
        if (slots.Select(s => s.HeroId).Distinct(StringComparer.Ordinal).Count() != slots.Count)
        {
            return TeamErrors.DuplicateHero;
        }

        // Rule 2 — mọi hero phải thuộc quyền sở hữu (không tin client).
        IReadOnlyList<OwnedHero> owned = await _ownedHeroes.GetByProfileIdAsync(profile.Id, cancellationToken);
        HashSet<string> ownedIds = owned.Select(h => h.HeroId).ToHashSet(StringComparer.Ordinal);
        if (slots.Any(s => !ownedIds.Contains(s.HeroId)))
        {
            return TeamErrors.HeroNotOwned;
        }

        List<TeamSlot> domainSlots = slots
            .Select(s => new TeamSlot(s.SlotIndex, s.HeroId))
            .ToList();

        DomainTeam? existing = await _teams.GetByProfileIdAsync(profile.Id, cancellationToken);
        if (existing is null)
        {
            DomainTeam team = DomainTeam.Create(Guid.NewGuid(), profile.Id, domainSlots, _clock.UtcNow);
            await _teams.AddAsync(team, cancellationToken);
            return Result.Success(TeamMapping.ToDto(team));
        }

        existing.Replace(domainSlots, _clock.UtcNow);
        return Result.Success(TeamMapping.ToDto(existing));
    }
}
