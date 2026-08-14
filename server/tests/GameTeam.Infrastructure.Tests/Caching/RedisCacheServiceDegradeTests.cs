using FluentAssertions;
using GameTeam.Infrastructure.Caching;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Caching;

/// <summary>
/// Chứng minh GRACEFUL DEGRADATION (Phase 12): khi Redis KHÔNG truy cập được, cache thao tác KHÔNG làm
/// sập request — <c>GetAsync</c> trả miss (null), <c>SetAsync</c>/<c>RemoveAsync</c> không ném, và có log
/// cảnh báo. Không cần container: multiplexer trỏ tới endpoint chết (<c>AbortOnConnectFail=false</c>).
/// </summary>
public sealed class RedisCacheServiceDegradeTests : IDisposable
{
    private readonly ConnectionMultiplexer _multiplexer;
    private readonly CapturingLogger<RedisCacheService> _logger = new();
    private readonly RedisCacheService _cache;

    public RedisCacheServiceDegradeTests()
    {
        // Endpoint không có server lắng nghe ⇒ mọi lệnh Redis đều lỗi kết nối.
        var options = new ConfigurationOptions
        {
            EndPoints = { "127.0.0.1:6399" },
            AbortOnConnectFail = false,
            ConnectRetry = 0,
            ConnectTimeout = 500,
        };
        _multiplexer = ConnectionMultiplexer.Connect(options);
        _cache = new RedisCacheService(_multiplexer, _logger, "test");
    }

    [Fact]
    public async Task Get_degrades_to_miss_and_logs_warning()
    {
        SampleDto? got = await _cache.GetAsync<SampleDto>("any-key", CancellationToken.None);

        got.Should().BeNull();
        _logger.HasWarning.Should().BeTrue();
    }

    [Fact]
    public async Task Set_does_not_throw_and_logs_warning()
    {
        Func<Task> act = () => _cache.SetAsync(
            "any-key", new SampleDto("v", 1), TimeSpan.FromMinutes(1), CancellationToken.None);

        await act.Should().NotThrowAsync();
        _logger.HasWarning.Should().BeTrue();
    }

    [Fact]
    public async Task Remove_does_not_throw_and_logs_warning()
    {
        Func<Task> act = () => _cache.RemoveAsync("any-key", CancellationToken.None);

        await act.Should().NotThrowAsync();
        _logger.HasWarning.Should().BeTrue();
    }

    public void Dispose() => _multiplexer.Dispose();

    public sealed record SampleDto(string Name, int Number);
}
