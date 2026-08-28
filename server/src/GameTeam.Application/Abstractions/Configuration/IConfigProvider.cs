using GameTeam.Contracts.Config;

namespace GameTeam.Application.Abstractions.Configuration;

/// <summary>
/// Configuration provider port (declared here, <b>implemented in Phase 21</b> — Config Service).
/// <para>
/// Code depends on the config <em>schema</em>, never on concrete balance values (ADR-004/005).
/// <c>CachingBehavior</c> folds <see cref="CurrentVersion"/> into cache keys so a config rollout
/// naturally invalidates stale cached reads. Phase 21 adds the runtime read path: the provider
/// serves entries from the <b>current published immutable bundle</b> (<c>config@vN</c>) held
/// in-memory, so Domain/Application read config <b>only</b> through this port — never the filesystem.
/// </para>
/// </summary>
public interface IConfigProvider
{
    /// <summary>Current config version (bundle + schema) — see <see cref="ConfigVersion"/>.</summary>
    ConfigVersion CurrentVersion { get; }

    /// <summary>
    /// Read a single config entry of <paramref name="type"/> (a config type name, e.g. <c>"hero"</c>)
    /// by its <paramref name="id"/> from the current bundle, deserialized into <typeparamref name="T"/>.
    /// Returns <c>null</c> when the type/id is absent. Synchronous + in-memory (no I/O) — safe on any
    /// hot path. The <paramref name="type"/> is a stable string (never the Infrastructure config-type
    /// enum) so Application/Domain stay decoupled from the loader.
    /// </summary>
    T? Get<T>(string type, string id)
        where T : class;

    /// <summary>Ids present for <paramref name="type"/> in the current bundle (empty when the type is absent).</summary>
    IReadOnlyList<string> GetIds(string type);
}
