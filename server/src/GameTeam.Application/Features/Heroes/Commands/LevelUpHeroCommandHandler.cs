using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Economy;
using GameTeam.Contracts.Enums;
using GameTeam.Contracts.Hero;
using GameTeam.Domain.Common;
using GameTeam.Domain.Heroes;
using MediatR;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;

namespace GameTeam.Application.Features.Heroes.Commands;

/// <summary>
/// Handles <see cref="LevelUpHeroCommand"/> — nâng cấp hero một cấp, server-authoritative + atomic
/// (ADR-004/007/011). Thứ tự: chủ sở hữu từ token (<see cref="ICurrentUser"/>) → profile → definition hero
/// + config economy (đường cong) → <b>khoá dòng hero</b> (FOR UPDATE) → kiểm trần cấp → tính chi phí (config)
/// → <b>tiêu gold</b> qua <see cref="CurrencyWalletService"/> (Phase 31, idempotent + ghi ledger) → tăng cấp
/// (<see cref="OwnedHero.LevelUp"/>) → tính lại chỉ số + Power (<see cref="HeroStatCalculator"/>). Tất cả trong
/// một transaction (TransactionBehavior): thiếu tiền/lỗi ⇒ rollback (không trừ tiền, không tăng cấp).
/// <para>
/// Idempotency của lần tiêu: khoá <c>hero-levelup:{profileId}:{ownedHeroId}:{targetLevel}</c> + khoá dòng hero
/// ⇒ dưới khoá, cấp đích luôn nhất quán với trạng thái dòng (không thể lên cấp "chùa"). Client chỉ gửi intent
/// + hiển thị kết quả — KHÔNG tự tăng cấp/chỉ số/Power/trừ tiền.
/// </para>
/// </summary>
public sealed class LevelUpHeroCommandHandler : IRequestHandler<LevelUpHeroCommand, Result<LevelUpHeroResponse>>
{
    private const string LevelUpSource = "hero_levelup";

    private readonly ICurrentUser _currentUser;
    private readonly IPlayerProfileRepository _profiles;
    private readonly IConfigProvider _config;
    private readonly IOwnedHeroRepository _ownedHeroes;
    private readonly CurrencyWalletService _wallet;

    public LevelUpHeroCommandHandler(
        ICurrentUser currentUser,
        IPlayerProfileRepository profiles,
        IConfigProvider config,
        IOwnedHeroRepository ownedHeroes,
        CurrencyWalletService wallet)
    {
        _currentUser = currentUser;
        _profiles = profiles;
        _config = config;
        _ownedHeroes = ownedHeroes;
        _wallet = wallet;
    }

    public async Task<Result<LevelUpHeroResponse>> Handle(
        LevelUpHeroCommand request, CancellationToken cancellationToken)
    {
        Guid? accountId = _currentUser.AccountId;
        if (accountId is null)
        {
            return HeroErrors.Unauthenticated;
        }

        DomainProfile? profile = await _profiles.GetByAccountIdAsync(accountId.Value, cancellationToken);
        if (profile is null)
        {
            // Không có profile ⇒ không sở hữu hero nào (không lộ tồn tại — chống IDOR).
            return HeroErrors.OwnedHeroNotFound;
        }

        HeroConfig? heroConfig = _config.Get<HeroConfig>(HeroMapping.ConfigType, request.HeroId);
        if (heroConfig is null)
        {
            return HeroErrors.DefinitionNotFound;
        }

        EconomyConfig? economy = _config.Get<EconomyConfig>(EconomyConfig.ConfigType, EconomyConfig.DefaultId);
        if (economy is null)
        {
            return HeroErrors.EconomyConfigNotFound;
        }

        // Khoá dòng hero (FOR UPDATE) — tuần tự hoá nâng cấp đồng thời; đọc cấp hiện tại nhất quán.
        OwnedHero? owned = await _ownedHeroes.GetByProfileAndHeroForUpdateAsync(
            profile.Id, request.HeroId, cancellationToken);
        if (owned is null)
        {
            return HeroErrors.OwnedHeroNotFound;
        }

        // Trần cấp phụ thuộc config: hết bước đường cong ⇒ không thể nâng (kiểm TRƯỚC khi tiêu).
        if (!HeroStatCalculator.TryGetLevelUpCost(economy, owned.Level, out long cost))
        {
            return HeroErrors.MaxLevel;
        }

        int targetLevel = owned.Level + 1;

        // Tiêu gold atomic + idempotent. Khoá mã hoá cấp đích + khoá dòng hero ⇒ nhất quán, chống double-spend.
        Result<CurrencyTransactionResult> spend = await _wallet.SpendAsync(
            profile.Id,
            Currency.Gold,
            cost,
            LevelUpSource,
            $"hero-levelup:{profile.Id}:{owned.Id}:{targetLevel}",
            cancellationToken);
        if (spend.IsFailure)
        {
            return Result.Failure<LevelUpHeroResponse>(spend.Error);
        }

        owned.LevelUp();

        int growthBp = economy.LevelStatGrowthBp;
        var stats = new HeroBaseStatsDto(
            HeroStatCalculator.ScaleStat(heroConfig.BaseStats.Hp, owned.Level, growthBp),
            HeroStatCalculator.ScaleStat(heroConfig.BaseStats.Atk, owned.Level, growthBp),
            HeroStatCalculator.ScaleStat(heroConfig.BaseStats.Def, owned.Level, growthBp),
            HeroStatCalculator.ScaleStat(heroConfig.BaseStats.Spd, owned.Level, growthBp));
        int power = HeroStatCalculator.Power(stats.Hp, stats.Atk, stats.Def, stats.Spd, economy.PowerWeights);

        var response = new LevelUpHeroResponse(
            owned.HeroId,
            owned.Level,
            stats,
            power,
            cost,
            spend.Value.BalanceAfter);
        return Result.Success(response);
    }
}
