using GameTeam.Application.Abstractions.Combat;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Economy;
using GameTeam.Application.Features.Heroes;
using GameTeam.Application.Features.Inventory;
using GameTeam.Contracts.Enums;
using GameTeam.Contracts.Summon;
using GameTeam.Domain.Common;
using GameTeam.Domain.Gacha;
using GameTeam.Domain.Heroes;
using MediatR;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;

namespace GameTeam.Application.Features.Summon;

/// <summary>
/// Handles <see cref="SummonCommand"/> — luồng triệu hồi đầu-cuối server-authoritative (ADR-004/007/011):
/// <list type="number">
///   <item>chủ sở hữu từ token (<see cref="ICurrentUser"/>) → profile;</item>
///   <item>kiểm count (1/10) + banner (config, data-driven) — dựng rarity→hero, cost, dupe, mục tiêu pity;</item>
///   <item><b>idempotency</b>: retry cùng <c>requestId</c> ⇒ trả kết quả đã lưu (không quay/tiêu/cấp lại);</item>
///   <item><b>khoá dòng pity</b> (FOR UPDATE) — tuần tự hoá lượt quay đồng thời cùng (profile, banner);</item>
///   <item>sinh seed server (<see cref="IBattleSeedSource"/>) → <see cref="SummonRoller"/> quyết rarity/hero/pity;</item>
///   <item>tiêu tiền qua <see cref="CurrencyWalletService"/> (Phase 31, atomic + idempotent + ledger) —
///     thiếu tiền ⇒ Result lỗi, rollback toàn bộ;</item>
///   <item>hero mới ⇒ cấp <see cref="OwnedHero"/> (Phase 27); trùng ⇒ gộp mảnh cấp qua
///     <see cref="InventoryService"/> (Phase 32) một lần (multi-item atomic);</item>
///   <item>cập nhật pity + ghi <see cref="SummonRecord"/> — tất cả trong <b>một transaction</b> (TransactionBehavior).</item>
/// </list>
/// Client chỉ gửi intent + hiển thị kết quả — KHÔNG tự random/quyết result (ADR-011). Seed lưu để audit, không trả client.
/// </summary>
public sealed class SummonCommandHandler : IRequestHandler<SummonCommand, Result<SummonResultDto>>
{
    private const string SummonSource = "gacha_summon";

    private static readonly int[] AllowedCounts = [1, 10];

    private readonly ICurrentUser _currentUser;
    private readonly IPlayerProfileRepository _profiles;
    private readonly IConfigProvider _config;
    private readonly ISummonRecordRepository _summonRecords;
    private readonly IGachaPityRepository _pity;
    private readonly IOwnedHeroRepository _ownedHeroes;
    private readonly CurrencyWalletService _wallet;
    private readonly InventoryService _inventory;
    private readonly IBattleSeedSource _seedSource;
    private readonly IClock _clock;

    public SummonCommandHandler(
        ICurrentUser currentUser,
        IPlayerProfileRepository profiles,
        IConfigProvider config,
        ISummonRecordRepository summonRecords,
        IGachaPityRepository pity,
        IOwnedHeroRepository ownedHeroes,
        CurrencyWalletService wallet,
        InventoryService inventory,
        IBattleSeedSource seedSource,
        IClock clock)
    {
        _currentUser = currentUser;
        _profiles = profiles;
        _config = config;
        _summonRecords = summonRecords;
        _pity = pity;
        _ownedHeroes = ownedHeroes;
        _wallet = wallet;
        _inventory = inventory;
        _seedSource = seedSource;
        _clock = clock;
    }

    public async Task<Result<SummonResultDto>> Handle(SummonCommand request, CancellationToken cancellationToken)
    {
        Guid? accountId = _currentUser.AccountId;
        if (accountId is null)
        {
            return SummonErrors.Unauthenticated;
        }

        DomainProfile? profile = await _profiles.GetByAccountIdAsync(accountId.Value, cancellationToken);
        if (profile is null)
        {
            return SummonErrors.ProfileNotFound;
        }

        if (!AllowedCounts.Contains(request.Count))
        {
            return SummonErrors.InvalidCount;
        }

        GachaConfig? banner = _config.Get<GachaConfig>(GachaMapping.ConfigType, request.BannerId);
        if (banner is null)
        {
            return SummonErrors.BannerNotFound;
        }

        // Dựng + kiểm banner TRƯỚC khi tiêu tiền (rate rarity có hero, cost hợp lệ, dupe phủ mọi rarity) ⇒
        // không bao giờ fail giữa transaction do config thiếu.
        Result<PreparedBanner> preparedResult = PrepareBanner(banner);
        if (preparedResult.IsFailure)
        {
            return Result.Failure<SummonResultDto>(preparedResult.Error);
        }

        PreparedBanner prepared = preparedResult.Value;

        // Idempotency (ADR-007): retry cùng requestId ⇒ trả kết quả đã lưu, KHÔNG quay/tiêu/cấp lần hai.
        SummonRecord? existing = await _summonRecords.GetByProfileAndRequestAsync(
            profile.Id, request.RequestId, cancellationToken);
        if (existing is not null)
        {
            return Result.Success(ToDto(existing));
        }

        DateTimeOffset now = _clock.UtcNow;

        // Khoá dòng pity (server-side, per profile+banner) — tuần tự hoá lượt quay đồng thời.
        GachaPity pityEntity = await LoadOrCreatePityForUpdateAsync(profile.Id, request.BannerId, now, cancellationToken);

        // Seed server (không để client chọn — ADR-011). Lưu để audit, không trả client.
        long seed = _seedSource.Next();
        SummonRoller.SummonRollOutcome outcome = SummonRoller.Roll(
            banner, prepared.HeroesByRarity, pityEntity.Count, request.Count, (ulong)seed);

        // Tiêu tiền (atomic + idempotent) — thiếu tiền ⇒ Result lỗi ⇒ rollback toàn bộ.
        long totalCost = prepared.UnitCost * request.Count;
        if (totalCost > 0)
        {
            Result<CurrencyTransactionResult> spend = await _wallet.SpendAsync(
                profile.Id, prepared.Currency, totalCost, SummonSource, $"gacha:{request.RequestId}:spend", cancellationToken);
            if (spend.IsFailure)
            {
                return Result.Failure<SummonResultDto>(spend.Error);
            }
        }

        // Phân giải hero mới vs trùng (server-authoritative). Trùng (đã sở hữu HOẶC đã trúng trước trong cùng
        // lượt) ⇒ mảnh; mới ⇒ cấp OwnedHero. Gộp mảnh để cấp MỘT lần (multi-item atomic, Phase 32).
        IReadOnlyList<OwnedHero> owned = await _ownedHeroes.GetByProfileIdAsync(profile.Id, cancellationToken);
        var ownedIds = new HashSet<string>(owned.Select(h => h.HeroId), StringComparer.Ordinal);

        var pullLines = new List<SummonPullLine>(outcome.Pulls.Count);
        var fragmentChanges = new List<ItemChange>();
        foreach (SummonRoller.RolledPull pull in outcome.Pulls)
        {
            if (ownedIds.Contains(pull.HeroId))
            {
                long fragments = prepared.DupeByRarity[pull.Rarity];
                if (fragments > 0)
                {
                    fragmentChanges.Add(new ItemChange(InventoryItemTypes.Fragment, pull.HeroId, fragments));
                }

                pullLines.Add(new SummonPullLine(pull.HeroId, pull.Rarity, isNew: false, fragments));
            }
            else
            {
                OwnedHero hero = OwnedHero.Grant(
                    Guid.NewGuid(), profile.Id, pull.HeroId, OwnedHero.InitialLevel, OwnedHero.InitialStars, now);
                await _ownedHeroes.AddAsync(hero, cancellationToken);
                ownedIds.Add(pull.HeroId);
                pullLines.Add(new SummonPullLine(pull.HeroId, pull.Rarity, isNew: true, fragments: 0));
            }
        }

        if (fragmentChanges.Count > 0)
        {
            Result<InventoryTransactionResult> grant = await _inventory.GrantAsync(
                profile.Id, fragmentChanges, SummonSource, $"gacha:{request.RequestId}:grant", cancellationToken);
            if (grant.IsFailure)
            {
                return Result.Failure<SummonResultDto>(grant.Error);
            }
        }

        // Cập nhật pity + ghi record (audit + idempotency) — cùng transaction.
        pityEntity.SetCount(outcome.EndingPity, now);

        SummonRecord record = SummonRecord.Create(
            Guid.NewGuid(),
            profile.Id,
            request.RequestId,
            request.BannerId,
            request.Count,
            seed,
            outcome.EndingPity,
            pullLines,
            now);
        await _summonRecords.AddAsync(record, cancellationToken);

        return Result.Success(ToDto(record));
    }

    private async Task<GachaPity> LoadOrCreatePityForUpdateAsync(
        Guid profileId, string bannerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        GachaPity? pity = await _pity.GetByProfileAndBannerForUpdateAsync(profileId, bannerId, cancellationToken);
        if (pity is not null)
        {
            return pity;
        }

        pity = GachaPity.CreateFor(Guid.NewGuid(), profileId, bannerId, now);
        await _pity.AddAsync(pity, cancellationToken);
        return pity;
    }

    /// <summary>
    /// Dựng bảng rarity→hero (từ hero config), cost, dupe theo rarity; kiểm banner hợp lệ để quay: có cost hợp
    /// lệ (loại tiền nhận diện được), mọi rarity <b>có thể sinh ra</b> (mọi rate rarity + mục tiêu pity khi bật)
    /// đều có ≥1 hero trong pool VÀ có mục dupe_fragments. Sai ⇒ <see cref="SummonErrors.InvalidBanner"/>.
    /// </summary>
    private Result<PreparedBanner> PrepareBanner(GachaConfig banner)
    {
        if (banner.Pool.Count == 0 || banner.Rates.Count == 0)
        {
            return Result.Failure<PreparedBanner>(SummonErrors.InvalidBanner);
        }

        if (banner.Cost is null || !CurrencyCode.TryParse(banner.Cost.Currency, out Currency currency) || banner.Cost.Amount < 0)
        {
            return Result.Failure<PreparedBanner>(SummonErrors.InvalidBanner);
        }

        // Rarity mỗi hero từ config (data-driven) — gom pool theo rarity, sort id để tất định.
        var byRarity = new Dictionary<int, List<string>>();
        foreach (string heroId in banner.Pool)
        {
            HeroConfig? hero = _config.Get<HeroConfig>(HeroMapping.ConfigType, heroId);
            if (hero is null)
            {
                return Result.Failure<PreparedBanner>(SummonErrors.InvalidBanner);
            }

            if (!byRarity.TryGetValue(hero.Rarity, out List<string>? list))
            {
                list = [];
                byRarity[hero.Rarity] = list;
            }

            list.Add(heroId);
        }

        var heroesByRarity = new Dictionary<int, IReadOnlyList<string>>();
        foreach ((int rarity, List<string> ids) in byRarity)
        {
            ids.Sort(StringComparer.Ordinal);
            heroesByRarity[rarity] = ids;
        }

        var dupeByRarity = new Dictionary<int, long>();
        foreach (GachaDupeFragment dupe in banner.DupeFragments)
        {
            dupeByRarity[dupe.Rarity] = dupe.Amount;
        }

        // Tập rarity CÓ THỂ SINH RA = mọi rate rarity (+ mục tiêu pity khi bật). Mỗi rarity phải có hero + dupe.
        var producible = new HashSet<int>(banner.Rates.Select(r => r.Rarity));
        bool pityEnabled = banner.Pity is { Enabled: true, Threshold: > 0 };
        if (pityEnabled)
        {
            int target = SummonRoller.ResolveTargetRarity(banner);
            if (target <= 0)
            {
                return Result.Failure<PreparedBanner>(SummonErrors.InvalidBanner);
            }

            producible.Add(target);
        }

        long totalWeight = banner.Rates.Sum(r => (long)r.Weight);
        if (totalWeight <= 0)
        {
            return Result.Failure<PreparedBanner>(SummonErrors.InvalidBanner);
        }

        foreach (int rarity in producible)
        {
            if (!heroesByRarity.TryGetValue(rarity, out IReadOnlyList<string>? ids) || ids.Count == 0)
            {
                return Result.Failure<PreparedBanner>(SummonErrors.InvalidBanner);
            }

            if (!dupeByRarity.ContainsKey(rarity))
            {
                return Result.Failure<PreparedBanner>(SummonErrors.InvalidBanner);
            }
        }

        return Result.Success(new PreparedBanner(heroesByRarity, dupeByRarity, currency, banner.Cost.Amount));
    }

    private static SummonResultDto ToDto(SummonRecord record)
        => new(
            record.BannerId,
            record.Count,
            record.PityAfter,
            record.Pulls.Select(p => new SummonPullDto(p.HeroId, p.Rarity, p.IsNew, p.Fragments)).ToList());

    private sealed record PreparedBanner(
        IReadOnlyDictionary<int, IReadOnlyList<string>> HeroesByRarity,
        IReadOnlyDictionary<int, long> DupeByRarity,
        Currency Currency,
        long UnitCost);
}
