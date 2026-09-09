namespace GameTeam.Application.Features.Battles;

/// <summary>
/// Bảng thưởng đọc từ config qua <c>IConfigProvider.Get&lt;RewardConfig&gt;("reward", id)</c> (data-driven —
/// ADR-004; bám <c>reward.schema.json</c>). Phase 30 cấp tối giản loại <c>currency</c>; loại khác (hero/fragment/
/// item) là phase 31–33. Giá trị thật (amount) là tuning — không hardcode.
/// </summary>
public sealed class RewardConfig
{
    /// <summary>Các khoản thưởng của bảng.</summary>
    public IReadOnlyList<RewardEntryConfig> Entries { get; init; } = Array.Empty<RewardEntryConfig>();
}

/// <summary>Một entry thưởng (bám <c>reward.schema.json</c> entries): loại + ref + số lượng.</summary>
public sealed class RewardEntryConfig
{
    /// <summary>Loại thưởng (<c>currency</c>/<c>hero</c>/<c>fragment</c>/<c>item</c>).</summary>
    public string RewardType { get; init; } = string.Empty;

    /// <summary>Khoá tham chiếu (vd <c>gold</c>).</summary>
    public string RefId { get; init; } = string.Empty;

    /// <summary>Số lượng (integer, ADR-011).</summary>
    public int Amount { get; init; }
}
