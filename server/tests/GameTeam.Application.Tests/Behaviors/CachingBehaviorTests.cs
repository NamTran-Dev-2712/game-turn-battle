using FluentAssertions;
using GameTeam.Application.Abstractions.Caching;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Behaviors;
using GameTeam.Application.Tests.TestSupport;
using GameTeam.Contracts.Config;
using GameTeam.Domain.Common;
using MediatR;
using NSubstitute;
using Xunit;

namespace GameTeam.Application.Tests.Behaviors;

/// <summary>
/// CachingBehavior: cache miss runs the handler and stores the successful result with TTL; cache hit
/// returns the cached result without running the handler; the cache key includes the config version;
/// non-cacheable requests bypass the cache entirely; failures are not cached.
/// </summary>
public sealed class CachingBehaviorTests
{
    private static readonly TimeSpan ExpectedTtl = TimeSpan.FromSeconds(1);

    private static (CachingBehavior<ProbeQuery, Result<string>> Behavior, ICacheService Cache) Build()
    {
        ICacheService cache = Substitute.For<ICacheService>();
        IConfigProvider config = Substitute.For<IConfigProvider>();
        config.CurrentVersion.Returns(new ConfigVersion(7, 1));
        return (new CachingBehavior<ProbeQuery, Result<string>>(cache, config), cache);
    }

    [Fact]
    public async Task Cache_miss_executes_handler_and_stores_result_with_ttl_and_config_versioned_key()
    {
        (CachingBehavior<ProbeQuery, Result<string>> behavior, ICacheService cache) = Build();
        cache.GetAsync<Result<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Result<string>?)null);

        bool handlerRan = false;
        RequestHandlerDelegate<Result<string>> next = () =>
        {
            handlerRan = true;
            return Task.FromResult(Result.Success("value"));
        };

        Result<string> result = await behavior.Handle(new ProbeQuery(), next, CancellationToken.None);

        handlerRan.Should().BeTrue();
        result.Value.Should().Be("value");
        await cache.Received(1).SetAsync(
            Arg.Is<string>(k => k.Contains("ProbeQuery") && k.Contains("probe") && k.Contains("cfg7")),
            Arg.Any<Result<string>>(),
            ExpectedTtl,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cache_hit_returns_cached_result_without_running_handler()
    {
        (CachingBehavior<ProbeQuery, Result<string>> behavior, ICacheService cache) = Build();
        Result<string> cached = Result.Success("cached-value");
        cache.GetAsync<Result<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cached);

        bool handlerRan = false;
        RequestHandlerDelegate<Result<string>> next = () =>
        {
            handlerRan = true;
            return Task.FromResult(Result.Success("fresh"));
        };

        Result<string> result = await behavior.Handle(new ProbeQuery(), next, CancellationToken.None);

        handlerRan.Should().BeFalse();
        result.Should().BeSameAs(cached);
        await cache.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<Result<string>>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failed_result_is_not_cached()
    {
        (CachingBehavior<ProbeQuery, Result<string>> behavior, ICacheService cache) = Build();
        cache.GetAsync<Result<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Result<string>?)null);

        RequestHandlerDelegate<Result<string>> next =
            () => Task.FromResult(Result.Failure<string>(new Error("NOPE", "no")));

        await behavior.Handle(new ProbeQuery(), next, CancellationToken.None);

        await cache.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<Result<string>>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Non_cacheable_request_bypasses_the_cache()
    {
        // ProbeCommand has no ICacheableQuery marker → CachingBehavior is never applied.
        using TestHost host = TestHost.Create();

        await host.Mediator.Send(new ProbeCommand(IsValid: true));

        host.Recorder.Steps.Should().NotContain("cache:get");
        host.Recorder.Steps.Should().NotContain("cache:set");
    }
}
