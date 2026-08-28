using GameTeam.Application.Abstractions.Caching;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Contracts.Config;
using GameTeam.Domain.Common;

namespace GameTeam.Application.Tests.TestSupport;

/// <summary>Unit of work that records begin/commit/rollback order into an <see cref="ExecutionRecorder"/>.</summary>
public sealed class RecordingUnitOfWork(ExecutionRecorder recorder) : IUnitOfWork
{
    public Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        recorder.Add("tx:begin");
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        recorder.Add("tx:commit");
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken)
    {
        recorder.Add("tx:rollback");
        return Task.CompletedTask;
    }
}

/// <summary>Cache that always misses and records get/set order into an <see cref="ExecutionRecorder"/>.</summary>
public sealed class RecordingCacheService(ExecutionRecorder recorder) : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        where T : class
    {
        recorder.Add("cache:get");
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        where T : class
    {
        recorder.Add("cache:set");
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        recorder.Add("cache:remove");
        return Task.CompletedTask;
    }
}

/// <summary>Config provider returning a fixed version; no entries (only the version is exercised here).</summary>
public sealed class FixedConfigProvider(ConfigVersion version) : IConfigProvider
{
    public ConfigVersion CurrentVersion { get; } = version;

    public T? Get<T>(string type, string id)
        where T : class => null;

    public IReadOnlyList<string> GetIds(string type) => [];
}

/// <summary>Deterministic clock for tests (no wall-clock).</summary>
public sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
