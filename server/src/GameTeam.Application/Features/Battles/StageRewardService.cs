using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Combat;
using GameTeam.Application.Features.Economy;
using GameTeam.Contracts.Enums;
using GameTeam.Domain.Battles;
using GameTeam.Domain.Common;

namespace GameTeam.Application.Features.Battles;

/// <summary>
/// Cấp <b>thưởng của một stage</b> (config-driven, ADR-004) — cơ chế dùng chung cho mọi luồng cấp thưởng stage
/// (battle thường phase 30, campaign phase 34). Đọc <c>stage.rewards[] → reward table</c>, chỉ cấp entry loại
/// <c>currency</c> (amount &gt; 0) qua <see cref="CurrencyWalletService"/> (atomic + idempotent + audit ledger —
/// Phase 31); gộp theo loại tiền rồi cấp MỘT lần/loại với khoá idempotency ổn định
/// <c>{idempotencyPrefix}:{code}</c>. Trả danh sách khoản <b>theo entry</b> để nhúng vào <see cref="BattleRecord"/>
/// + trả client. Loại thưởng khác (hero/fragment/item) là scope phase sau (bỏ qua ở đây).
/// <para>
/// Service <b>không</b> tự mở transaction (chạy trong transaction của command top-level qua
/// <c>TransactionBehavior</c>). Người gọi quyết định <b>khi nào</b> cấp (vd chỉ khi VICTORY; campaign thêm điều
/// kiện first-clear) — service chỉ thực hiện việc cấp.
/// </para>
/// </summary>
public sealed class StageRewardService
{
    private const string RewardConfigType = "reward";
    private const string CurrencyRewardType = "currency";

    private readonly IConfigProvider _config;
    private readonly CurrencyWalletService _wallet;

    public StageRewardService(IConfigProvider config, CurrencyWalletService wallet)
    {
        _config = Guard.NotNull(config);
        _wallet = Guard.NotNull(wallet);
    }

    /// <summary>
    /// Cấp toàn bộ thưởng <c>currency</c> của <paramref name="stageId"/> cho <paramref name="profileId"/>.
    /// Khoá idempotency mỗi loại tiền = <c>{idempotencyPrefix}:{code}</c> ⇒ retry an toàn (không cấp lần hai).
    /// Trả danh sách theo entry (rỗng nếu stage không có thưởng currency).
    /// </summary>
    public async Task<IReadOnlyList<BattleReward>> GrantAsync(
        string stageId, Guid profileId, string source, string idempotencyPrefix, CancellationToken cancellationToken)
    {
        var granted = new List<BattleReward>();

        StageCombatConfig? stage = _config.Get<StageCombatConfig>(CombatInputResolver.StageType, stageId);
        if (stage is null || stage.Rewards.Count == 0)
        {
            return granted;
        }

        // Gộp số lượng theo loại tiền (một lần cấp/loại), vẫn giữ danh sách theo entry cho record/DTO.
        var creditByCurrency = new Dictionary<Currency, long>();
        foreach (string rewardId in stage.Rewards)
        {
            RewardConfig? rewardConfig = _config.Get<RewardConfig>(RewardConfigType, rewardId);
            if (rewardConfig is null)
            {
                continue; // referential integrity kiểm ở config-validator (phase 07); an toàn thì bỏ qua.
            }

            foreach (RewardEntryConfig entry in rewardConfig.Entries)
            {
                if (!string.Equals(entry.RewardType, CurrencyRewardType, StringComparison.Ordinal) || entry.Amount <= 0)
                {
                    continue;
                }

                if (!CurrencyCode.TryParse(entry.RefId, out Currency currency))
                {
                    continue; // refId không phải loại tiền nhận diện được (config lạ) — bỏ qua an toàn.
                }

                creditByCurrency[currency] = creditByCurrency.GetValueOrDefault(currency) + entry.Amount;
                granted.Add(new BattleReward(entry.RewardType, entry.RefId, entry.Amount));
            }
        }

        foreach ((Currency currency, long total) in creditByCurrency)
        {
            string idempotencyKey = $"{idempotencyPrefix}:{CurrencyCode.ToCode(currency)}";
            _ = await _wallet.GrantAsync(profileId, currency, total, source, idempotencyKey, cancellationToken);
        }

        return granted;
    }
}
