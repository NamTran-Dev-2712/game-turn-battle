using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Heroes;
using GameTeam.Application.Features.Inventory;
using GameTeam.Contracts.Auth;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Common;
using GameTeam.Domain.Heroes;
using GameTeam.Domain.Profiles;
using MediatR;

namespace GameTeam.Application.Features.Auth.Commands;

/// <summary>
/// Handles <see cref="CreateGuestAccountCommand"/>. Thin: creates the guest aggregate and its
/// server-authoritative <see cref="PlayerProfile"/> (ADR-007) in the SAME transaction, stages both for
/// persistence (committed by the transaction behavior), and issues tokens via <see cref="ITokenService"/>.
/// The account id is generated here so the JWT <c>sub</c> is known without a database round-trip; the
/// server clock (<see cref="IClock"/>) stamps creation time — never wall-clock.
/// <para>
/// Eager, atomic profile creation satisfies "guest login → profile created" and guarantees exactly one
/// profile per account (the unique <c>account_id</c> index is the DB-level idempotency backstop).
/// </para>
/// </summary>
public sealed class CreateGuestAccountCommandHandler
    : IRequestHandler<CreateGuestAccountCommand, Result<AuthGuestResponse>>
{
    /// <summary>Seed TẠM số lượng mỗi vật phẩm catalog cho guest mới (Phase 32 — chưa có gacha/shop).</summary>
    private const long StarterItemQuantity = 10;

    /// <summary>Seed TẠM số lượng mảnh mỗi hero cho guest mới (Phase 32 — chưa có gacha/ascension).</summary>
    private const long StarterFragmentQuantity = 20;

    private readonly IRepository<Account, Guid> _accounts;
    private readonly IPlayerProfileRepository _profiles;
    private readonly IOwnedHeroRepository _ownedHeroes;
    private readonly InventoryService _inventory;
    private readonly IConfigProvider _config;
    private readonly ITokenService _tokenService;
    private readonly IClock _clock;

    public CreateGuestAccountCommandHandler(
        IRepository<Account, Guid> accounts,
        IPlayerProfileRepository profiles,
        IOwnedHeroRepository ownedHeroes,
        InventoryService inventory,
        IConfigProvider config,
        ITokenService tokenService,
        IClock clock)
    {
        _accounts = accounts;
        _profiles = profiles;
        _ownedHeroes = ownedHeroes;
        _inventory = inventory;
        _config = config;
        _tokenService = tokenService;
        _clock = clock;
    }

    public async Task<Result<AuthGuestResponse>> Handle(
        CreateGuestAccountCommand request,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = _clock.UtcNow;

        Account account = Account.CreateGuest(Guid.NewGuid(), now);
        await _accounts.AddAsync(account, cancellationToken);

        PlayerProfile profile = PlayerProfile.CreateForAccount(Guid.NewGuid(), account.Id, now);
        await _profiles.AddAsync(profile, cancellationToken);

        // Seed TẠM (Phase 27): cấp cho guest mới toàn bộ hero có trong config hiện hành (data-driven —
        // thêm hero vào config ⇒ guest mới tự sở hữu, KHÔNG sửa code). Đây KHÔNG phải cơ chế nhận thật;
        // nhận hero thật (summon) ở phase 33. Config trống ⇒ không cấp gì (graceful). Cùng transaction với
        // account+profile ⇒ nguyên tử.
        foreach (string heroId in _config.GetIds(HeroMapping.ConfigType))
        {
            OwnedHero hero = OwnedHero.Grant(
                Guid.NewGuid(), profile.Id, heroId, OwnedHero.InitialLevel, OwnedHero.InitialStars, now);
            await _ownedHeroes.AddAsync(hero, cancellationToken);
        }

        // Seed TẠM (Phase 32): cấp cho guest mới một ít vật phẩm catalog + mảnh mỗi hero để màn kho có dữ liệu
        // hiển thị. Data-driven (đọc config) — KHÔNG phải cơ chế nhận thật; nhận thật (gacha/shop) ở phase
        // 33/40. Đi qua InventoryService (atomic + idempotency + ledger) trong CÙNG transaction với
        // account+profile+hero. Config trống ⇒ danh sách rỗng ⇒ KHÔNG gọi (tránh Result lỗi UnknownItem).
        var starter = new List<ItemChange>();
        foreach (string itemId in _config.GetIds(InventoryItemTypes.ItemConfigType))
        {
            starter.Add(new ItemChange(InventoryItemTypes.Item, itemId, StarterItemQuantity));
        }

        foreach (string heroId in _config.GetIds(HeroMapping.ConfigType))
        {
            starter.Add(new ItemChange(InventoryItemTypes.Fragment, heroId, StarterFragmentQuantity));
        }

        if (starter.Count > 0)
        {
            await _inventory.GrantAsync(
                profile.Id, starter, "seed_starter", $"seed:{profile.Id}:starter", cancellationToken);
        }

        TokenBundle tokens = _tokenService.CreateTokens(account.Id, account.Type);

        return Result.Success(new AuthGuestResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresInSeconds));
    }
}
