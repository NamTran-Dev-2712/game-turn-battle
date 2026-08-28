namespace GameTeam.Infrastructure.Configuration;

/// <summary>
/// Options for the Configuration Service (Phase 21) — bound from section <c>"ConfigService"</c>
/// (Options pattern, like <see cref="Auth.JwtOptions"/>). Points the publish pipeline at the
/// author-time config tree and the JSON Schema set. Both default to their repo-relative locations
/// and are resolved to absolute paths at publish time (see <see cref="ConfigPathResolver"/>), so the
/// service works regardless of the process working directory; tests inject absolute temp paths.
/// </summary>
public sealed class ConfigServiceOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ConfigService";

    /// <summary>Author-time config root (default <c>config</c>).</summary>
    public string ConfigRoot { get; init; } = "config";

    /// <summary>JSON Schema root reused by the Phase-07 validator (default <c>shared/config-schema</c>).</summary>
    public string SchemaRoot { get; init; } = "shared/config-schema";
}
