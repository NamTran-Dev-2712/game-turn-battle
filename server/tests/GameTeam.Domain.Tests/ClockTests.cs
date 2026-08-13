using System;
using System.Reflection;
using FluentAssertions;
using GameTeam.Domain.Common;
using Xunit;

namespace GameTeam.Domain.Tests;

public class ClockTests
{
    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    [Fact]
    public void IClock_exposes_datetimeoffset_utcnow()
    {
        PropertyInfo? property = typeof(IClock).GetProperty(nameof(IClock.UtcNow));

        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<DateTimeOffset>();
    }

    [Fact]
    public void Injected_clock_returns_the_provided_instant()
    {
        var instant = new DateTimeOffset(2026, 8, 12, 10, 30, 0, TimeSpan.Zero);

        IClock clock = new FixedClock(instant);

        clock.UtcNow.Should().Be(instant);
    }
}
