using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Contracts.Config;

namespace GameTeam.Infrastructure.Configuration;

/// <summary>
/// Minimal <see cref="IConfigProvider"/> so the composition root is complete and the caching pipeline
/// (<c>CachingBehavior</c>) can fold a config version into cache keys — required the moment the first
/// <c>ICacheableQuery</c> endpoint (Phase 13 <c>/api/v1/server-time</c>) is served over HTTP.
/// <para>
/// It reports a fixed <c>config@v1</c> version (bundle = 1, schema = 1), aligned with the Phase-11
/// <c>schema_metadata</c> seed and the Phase-06 config bundle. It reads NO config values — that is
/// <b>Phase 21</b> (Config Service), which REPLACES this placeholder with real bundle loading /
/// publishing. Do not add balance/config-reading logic here (ADR-004/005).
/// </para>
/// </summary>
public sealed class DefaultConfigProvider : IConfigProvider
{
    /// <summary>Fixed placeholder version until Phase 21 wires the real Config Service.</summary>
    public ConfigVersion CurrentVersion { get; } = new(Bundle: 1, Schema: 1);
}
