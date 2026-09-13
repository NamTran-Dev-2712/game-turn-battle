using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Inventory;
using GameTeam.Contracts.Auth;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Common;
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
/// <para>
/// <b>Hero/fragment acquisition is summon (Phase 33).</b> Guest mới KHÔNG được cấp sẵn hero/mảnh — nhận hero
/// thật qua triệu hồi (gacha). (Trước Phase 33 có seed TẠM cấp toàn bộ hero + mảnh; đã gỡ để summon là đường
/// nhận thật.) Chỉ giữ seed vật phẩm catalog để màn kho có dữ liệu hiển thị tới khi có shop (Phase 40).
/// </para>
/// </summary>
public sealed class CreateGuestAccountCommandHandler
    : IRequestHandler<CreateGuestAccountCommand, Result<AuthGuestResponse>>
{
    /// <summary>Seed TẠM số lượng mỗi vật phẩm catalog cho guest mới (chưa có shop — Phase 40).</summary>
    private const long StarterItemQuantity = 10;

    private readonly IRepository<Account, Guid> _accounts;
    private readonly IPlayerProfileRepository _profiles;
    private readonly InventoryService _inventory;
    private readonly IConfigProvider _config;
    private readonly ITokenService _tokenService;
    private readonly IClock _clock;

    public CreateGuestAccountCommandHandler(
        IRepository<Account, Guid> accounts,
        IPlayerProfileRepository profiles,
        InventoryService inventory,
        IConfigProvider config,
        ITokenService tokenService,
        IClock clock)
    {
        _accounts = accounts;
        _profiles = profiles;
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

        // Seed TẠM (Phase 32): cấp cho guest mới một ít vật phẩm catalog để màn kho có dữ liệu hiển thị. Data-driven
        // (đọc config) — KHÔNG phải cơ chế nhận thật; nhận thật (shop) ở phase 40. Hero/mảnh KHÔNG seed nữa —
        // nhận hero/mảnh qua triệu hồi (Phase 33). Đi qua InventoryService (atomic + idempotency + ledger) trong
        // CÙNG transaction với account+profile. Config trống ⇒ danh sách rỗng ⇒ KHÔNG gọi (tránh Result UnknownItem).
        var starter = new List<ItemChange>();
        foreach (string itemId in _config.GetIds(InventoryItemTypes.ItemConfigType))
        {
            starter.Add(new ItemChange(InventoryItemTypes.Item, itemId, StarterItemQuantity));
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
