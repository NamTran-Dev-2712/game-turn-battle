using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Contracts.Hero;
using GameTeam.Domain.Common;
using GameTeam.Domain.Heroes;
using MediatR;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;

namespace GameTeam.Application.Features.Heroes.Queries;

/// <summary>
/// Handles <see cref="GetMyHeroesQuery"/>: chủ sở hữu suy từ <see cref="ICurrentUser"/> → profile theo
/// account → hero owned theo profile → map DTO. Read-only, KHÔNG mutate/tạo. Chưa có profile ⇒ danh sách
/// rỗng (chưa sở hữu gì); chưa xác thực ⇒ lỗi.
/// </summary>
public sealed class GetMyHeroesQueryHandler
    : IRequestHandler<GetMyHeroesQuery, Result<MyHeroesResponse>>
{
    private readonly IOwnedHeroRepository _ownedHeroes;
    private readonly IPlayerProfileRepository _profiles;
    private readonly ICurrentUser _currentUser;

    public GetMyHeroesQueryHandler(
        IOwnedHeroRepository ownedHeroes,
        IPlayerProfileRepository profiles,
        ICurrentUser currentUser)
    {
        _ownedHeroes = ownedHeroes;
        _profiles = profiles;
        _currentUser = currentUser;
    }

    public async Task<Result<MyHeroesResponse>> Handle(
        GetMyHeroesQuery request,
        CancellationToken cancellationToken)
    {
        Guid? accountId = _currentUser.AccountId;
        if (accountId is null)
        {
            return HeroErrors.Unauthenticated;
        }

        DomainProfile? profile = await _profiles.GetByAccountIdAsync(accountId.Value, cancellationToken);
        if (profile is null)
        {
            return Result.Success(new MyHeroesResponse(Array.Empty<OwnedHeroDto>()));
        }

        IReadOnlyList<OwnedHero> owned = await _ownedHeroes.GetByProfileIdAsync(profile.Id, cancellationToken);
        IReadOnlyList<OwnedHeroDto> dtos = owned.Select(HeroMapping.ToDto).ToList();

        return Result.Success(new MyHeroesResponse(dtos));
    }
}
