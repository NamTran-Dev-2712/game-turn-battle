using FluentAssertions;
using GameTeam.Application;
using GameTeam.Application.Abstractions.Caching;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Contracts.Config;
using GameTeam.Domain.Common;
using GameTeam.Infrastructure.Caching;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StackExchange.Redis;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Caching;

/// <summary>
/// Tiêu chí đóng Phase 12: <c>CachingBehavior</c> (Phase 10) chạy THẬT với Redis. Chạy cùng một
/// <see cref="ICacheableQuery"/> hai lần qua MediatR với <see cref="ICacheService"/> = <see cref="RedisCacheService"/>
/// (Redis Testcontainers): lần 1 miss ⇒ handler chạy + set cache; lần 2 HIT ⇒ handler KHÔNG chạy
/// (đếm invocation không đổi). Chứng minh flow Request→MediatR→CachingBehavior→ICacheService→Redis.
/// </summary>
public sealed class CachingBehaviorRedisIntegrationTests : IClassFixture<RedisContainerFixture>, IDisposable
{
    private readonly ConnectionMultiplexer _multiplexer;
    private readonly ServiceProvider _provider;

    public CachingBehaviorRedisIntegrationTests(RedisContainerFixture fixture)
    {
        ConfigurationOptions options = ConfigurationOptions.Parse(fixture.ConnectionString);
        options.AbortOnConnectFail = false;
        _multiplexer = ConnectionMultiplexer.Connect(options);

        IConfigProvider configProvider = Substitute.For<IConfigProvider>();
        configProvider.CurrentVersion.Returns(new ConfigVersion(1, 1));

        var services = new ServiceCollection();

        // Composition Application thật (MediatR + FluentValidation + 4 behaviors đúng thứ tự).
        services.AddApplication();
        // Handler probe nằm ở assembly test — đăng ký thêm (như TestHost của Application.Tests).
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CachingBehaviorRedisIntegrationTests).Assembly));
        services.AddLogging();

        services.AddSingleton(new HandlerInvocationCounter());
        services.AddSingleton<IConfigProvider>(configProvider);
        services.AddSingleton<ICacheService>(new RedisCacheService(
            _multiplexer,
            new CapturingLogger<RedisCacheService>(),
            "test"));

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Second_identical_query_is_cache_hit_and_skips_handler()
    {
        IMediator mediator = _provider.GetRequiredService<IMediator>();
        HandlerInvocationCounter counter = _provider.GetRequiredService<HandlerInvocationCounter>();

        Result<string> first = await mediator.Send(new CountingCachedQuery());
        Result<string> second = await mediator.Send(new CountingCachedQuery());

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Value.Should().Be(first.Value);

        // Lần 2 phải là cache hit ⇒ handler chỉ chạy đúng 1 lần.
        counter.Count.Should().Be(1);
    }

    public void Dispose()
    {
        _provider.Dispose();
        _multiplexer.Dispose();
    }
}

/// <summary>Bộ đếm số lần handler thực thi (chứng minh cache hit bỏ qua handler).</summary>
public sealed class HandlerInvocationCounter
{
    private int _count;

    public int Count => _count;

    public void Increment() => Interlocked.Increment(ref _count);
}

/// <summary>Query cacheable probe với TTL rộng (không phụ thuộc timing) để test cache hit qua Redis.</summary>
public sealed record CountingCachedQuery : IRequest<Result<string>>, ICacheableQuery
{
    public string CacheKey => "counting";

    public TimeSpan CacheTtl => TimeSpan.FromMinutes(5);
}

/// <summary>Handler đếm số lần chạy; trả payload cố định.</summary>
public sealed class CountingCachedQueryHandler : IRequestHandler<CountingCachedQuery, Result<string>>
{
    private readonly HandlerInvocationCounter _counter;

    public CountingCachedQueryHandler(HandlerInvocationCounter counter) => _counter = counter;

    public Task<Result<string>> Handle(CountingCachedQuery request, CancellationToken cancellationToken)
    {
        _counter.Increment();
        return Task.FromResult(Result.Success("cached-payload"));
    }
}
