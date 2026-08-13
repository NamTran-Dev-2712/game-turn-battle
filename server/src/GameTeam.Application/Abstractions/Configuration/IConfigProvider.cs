using GameTeam.Contracts.Config;

namespace GameTeam.Application.Abstractions.Configuration;

/// <summary>
/// Configuration provider port (declared here, <b>implemented in Phase 21</b> — Config Service).
/// <para>
/// Code depends on the config <em>schema</em>, never on concrete balance values (ADR-004/005). In
/// Phase 10 only <see cref="CurrentVersion"/> is needed: <c>CachingBehavior</c> folds the config
/// bundle version into cache keys so a config rollout naturally invalidates stale cached reads.
/// Reading typed config bundles is added when Phase 21 implements this port.
/// </para>
/// </summary>
public interface IConfigProvider
{
    /// <summary>Current config version (bundle + schema) — see <see cref="ConfigVersion"/>.</summary>
    ConfigVersion CurrentVersion { get; }
}
