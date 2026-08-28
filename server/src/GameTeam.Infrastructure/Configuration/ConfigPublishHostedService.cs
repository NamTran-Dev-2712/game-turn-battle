using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameTeam.Infrastructure.Configuration;

/// <summary>
/// Deploy-time publish (Phase 21 MVP): runs the config publish pipeline ONCE at application startup, so
/// deploying the app publishes the current config bundle (ADR-005). The pipeline is idempotent — an
/// unchanged config is a no-op; a changed config bumps the version.
/// <para>
/// <b>Resilient by design</b> (graceful degradation, like the Redis cache): any failure at boot (DB not
/// migrated/reachable, etc.) is logged and swallowed — it never crashes the host. The provider then
/// serves the last-published bundle if one exists, or nothing until a successful publish.
/// </para>
/// </summary>
public sealed class ConfigPublishHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConfigPublishHostedService> _logger;

    public ConfigPublishHostedService(IServiceScopeFactory scopeFactory, ILogger<ConfigPublishHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // The publisher + its DbContext/store are scoped ⇒ create a scope for the one-shot publish.
            using IServiceScope scope = _scopeFactory.CreateScope();
            ConfigBundlePublisher publisher = scope.ServiceProvider.GetRequiredService<ConfigBundlePublisher>();

            PublishResult result = await publisher.PublishAsync(cancellationToken);
            _logger.LogInformation(
                "Config publish on startup: {Reason} (config@v{Version}).", result.Reason, result.Version);
        }
        catch (Exception ex)
        {
            // Deliberately broad at the boot boundary: deploy publish is best-effort and MUST NOT crash
            // the host (graceful degradation, like the Redis cache). Logged at Warning — e.g. build-time
            // OpenAPI generation boots the host with no DB reachable; the provider then serves the
            // last-published bundle if one exists.
            _logger.LogWarning(ex, "Config publish on startup skipped; serving last-published bundle if available.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
