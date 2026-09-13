namespace GameTeam.Application.Features.Summon;

/// <summary>Hằng loại config gacha trong bundle (data-driven — dùng cho <c>IConfigProvider</c>).</summary>
internal static class GachaMapping
{
    /// <summary>Khoá loại config của banner gacha (khớp thư mục <c>config/gacha</c> + <c>gacha.schema.json</c>).</summary>
    public const string ConfigType = "gacha";
}
