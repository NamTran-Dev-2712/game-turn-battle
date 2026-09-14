namespace GameTeam.Application.Features.Economy;

/// <summary>
/// POCO đọc cấu hình economy từ config qua <c>IConfigProvider.Get&lt;EconomyConfig&gt;("economy", id)</c>
/// (data-driven, ADR-004). Khoá JSON là <c>snake_case</c> (map bằng naming policy của
/// <c>RuntimeConfigProvider</c>). Chứa <b>đường cong</b> (tuning) — KHÔNG hardcode số ở code:
/// <see cref="CostCurves"/> (vd <c>level_up</c> = chi phí gold mỗi cấp), <see cref="LevelStatGrowthBp"/>
/// (tăng trưởng chỉ số/cấp, basis points), <see cref="PowerWeights"/> (trọng số Power Rating). Thiếu config ⇒
/// handler báo lỗi (không đoán mặc định). Bám schema economy (phase 06, mở rộng additive ở phase 35).
/// </summary>
public sealed class EconomyConfig
{
    /// <summary>Khoá loại config economy trong bundle (data-driven — dùng cho <c>IConfigProvider</c>).</summary>
    public const string ConfigType = "economy";

    /// <summary>Id cấu hình economy mặc định (một bản MVP, như <c>formation_default</c>).</summary>
    public const string DefaultId = "economy_default";

    /// <summary>Tên đường cong chi phí lên cấp hero.</summary>
    public const string LevelUpCurve = "level_up";

    /// <summary>Id definition.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Ánh xạ tên đường cong → mảng bước chi phí integer. Với <c>level_up</c>: phần tử thứ i là chi phí (gold)
    /// để lên từ cấp (i+1) sang (i+2) ⇒ số cấp tối đa = 1 + độ dài mảng.
    /// </summary>
    public Dictionary<string, List<int>> CostCurves { get; init; } = new();

    /// <summary>
    /// Tăng trưởng chỉ số mỗi cấp theo basis points của chỉ số nền (vd 800 = +8%/cấp). Áp đều hp/atk/def/spd.
    /// </summary>
    public int LevelStatGrowthBp { get; init; }

    /// <summary>Trọng số integer cho Power Rating (tổng có trọng số của chỉ số cuối).</summary>
    public PowerWeightsConfig PowerWeights { get; init; } = new();
}

/// <summary>Trọng số Power Rating (integer, ADR-011) — lát cắt của <see cref="EconomyConfig"/>.</summary>
public sealed class PowerWeightsConfig
{
    /// <summary>Trọng số máu.</summary>
    public int Hp { get; init; }

    /// <summary>Trọng số tấn công.</summary>
    public int Atk { get; init; }

    /// <summary>Trọng số phòng thủ.</summary>
    public int Def { get; init; }

    /// <summary>Trọng số tốc độ.</summary>
    public int Spd { get; init; }
}
