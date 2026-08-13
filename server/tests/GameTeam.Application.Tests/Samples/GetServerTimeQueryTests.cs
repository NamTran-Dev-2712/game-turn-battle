using FluentAssertions;
using GameTeam.Application.Features.Diagnostics.Queries;
using GameTeam.Application.Tests.TestSupport;
using GameTeam.Domain.Common;
using Xunit;

namespace GameTeam.Application.Tests.Samples;

/// <summary>
/// End-to-end: the sample <see cref="GetServerTimeQuery"/> flows through the real MediatR pipeline,
/// reads time via the injected <c>IClock</c> (not the system clock), and exercises the cache pipeline.
/// </summary>
public sealed class GetServerTimeQueryTests
{
    [Fact]
    public async Task Returns_time_from_the_injected_clock_and_goes_through_the_cache_pipeline()
    {
        var fixedNow = new DateTimeOffset(2026, 8, 13, 12, 34, 56, TimeSpan.Zero);
        using TestHost host = TestHost.Create(clockUtcNow: fixedNow);

        Result<ServerTimeResponse> result = await host.Mediator.Send(new GetServerTimeQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.UtcNow.Should().Be(fixedNow);

        // Cache miss (recording cache always misses) → handler runs, result stored.
        // (The real GetServerTimeQueryHandler does not touch the recorder — only the cache does.)
        host.Recorder.Steps.Should().Equal("cache:get", "cache:set");
    }
}
