using System.Text.Json.Nodes;
using GameTeam.ConfigValidator;
using GameTeam.Contracts.Config;
using GameTeam.Domain.Common;
using GameTeam.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameTeam.Infrastructure.Configuration;

/// <summary>Outcome of a publish attempt.</summary>
/// <param name="Published">True when a NEW version was published; false when unchanged or blocked.</param>
/// <param name="Version">The resulting current version number (unchanged on a blocked publish).</param>
/// <param name="Reason">One of <c>published</c> / <c>unchanged</c> / <c>validation-failed</c>.</param>
public sealed record PublishResult(bool Published, int Version, string Reason);

/// <summary>
/// The Configuration Service publish pipeline (Phase 21, ADR-005): load <c>config/</c> → validate
/// (reuse the Phase-07 <see cref="ConfigValidationRunner"/> — one validation source of truth) → build
/// an immutable bundle with a deterministic checksum → dedup vs the current bundle → on change persist +
/// cache + flip "current" atomically → refresh the in-memory <see cref="RuntimeConfigProvider"/>.
/// <para>
/// <b>Safety:</b> a validation failure aborts before anything is persisted/cached/flipped — "current"
/// is left untouched and the invalid bundle is never served. An unchanged config (same checksum) does
/// not bump the version, so redeploying identical config is idempotent.
/// </para>
/// </summary>
public sealed class ConfigBundlePublisher
{
    private readonly ConfigBundleStore _store;
    private readonly RuntimeConfigProvider _provider;
    private readonly IClock _clock;
    private readonly ConfigServiceOptions _options;
    private readonly ILogger<ConfigBundlePublisher> _logger;

    public ConfigBundlePublisher(
        ConfigBundleStore store,
        RuntimeConfigProvider provider,
        IClock clock,
        IOptions<ConfigServiceOptions> options,
        ILogger<ConfigBundlePublisher> logger)
    {
        _store = store;
        _provider = provider;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Run the pipeline once. Idempotent w.r.t. unchanged config.</summary>
    public async Task<PublishResult> PublishAsync(CancellationToken cancellationToken)
    {
        string configRoot = ConfigPathResolver.Resolve(_options.ConfigRoot);
        string schemaRoot = ConfigPathResolver.Resolve(_options.SchemaRoot);
        string reportBase = ConfigPathResolver.ReportBaseFor(configRoot);

        // 1) Validate FIRST (reuse Phase-07 core lib). Fail ⇒ do NOT publish; serve last-good if any.
        ValidationReport report = ConfigValidationRunner.Run(new ConfigValidatorOptions(configRoot, schemaRoot, reportBase));
        if (!report.IsValid)
        {
            _logger.LogError(
                "Config validation failed ({ErrorCount} error(s)); NOT publishing. First: {FirstError}",
                report.Errors.Count,
                report.Errors.Count > 0 ? report.Errors[0].ToString() : "(none)");

            await LoadCurrentIntoProviderAsync(cancellationToken);
            return new PublishResult(false, _provider.CurrentVersion.Bundle, "validation-failed");
        }

        // 2) Build content + deterministic checksum.
        int schemaVersion = VersionValidator.SupportedSchemaVersion;
        LoadedConfig loaded = ConfigLoader.Load(configRoot, reportBase);
        JsonObject data = ConfigBundleBuilder.BuildData(loaded.Entities);
        string checksum = ConfigBundleBuilder.ComputeChecksum(schemaVersion, data);

        // 3) Dedup vs current — identical config ⇒ no new version (idempotent redeploy).
        StoredBundle? current = await _store.GetCurrentAsync(cancellationToken);
        if (current is not null && string.Equals(current.Checksum, checksum, StringComparison.Ordinal))
        {
            _provider.Apply(ConfigSnapshot.FromPayload(current.Payload));
            return new PublishResult(false, current.Version, "unchanged");
        }

        // 4) New version = current + 1.
        int newVersion = (current?.Version ?? 0) + 1;
        string label = ConfigVersionLabel.For(newVersion);
        DateTimeOffset generatedAt = _clock.UtcNow;
        string payload = ConfigBundleBuilder.ComposeBundleJson(schemaVersion, label, checksum, generatedAt, data);
        ConfigBundleRecord record = new(newVersion, label, schemaVersion, checksum, generatedAt, payload);

        // 5) Atomic persist + cache + flip "current" (only after the bundle is fully built).
        await _store.SaveAndPublishAsync(record, cancellationToken);

        // 6) Refresh the in-memory provider from the just-built data.
        _provider.Apply(ConfigSnapshot.FromData(new ConfigVersion(newVersion, schemaVersion), data));

        _logger.LogInformation("Published config bundle {Label} (checksum {Checksum}).", label, checksum);
        return new PublishResult(true, newVersion, "published");
    }

    private async Task LoadCurrentIntoProviderAsync(CancellationToken cancellationToken)
    {
        StoredBundle? current = await _store.GetCurrentAsync(cancellationToken);
        if (current is not null)
        {
            _provider.Apply(ConfigSnapshot.FromPayload(current.Payload));
        }
    }
}
