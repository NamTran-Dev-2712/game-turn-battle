using FluentAssertions;
using Xunit;

namespace GameTeam.Domain.Tests;

/// <summary>Smoke test bootstrap — xác nhận project & tham chiếu tầng Domain hoạt động.</summary>
public class SmokeTests
{
    [Fact]
    public void Domain_assembly_is_reachable()
    {
        typeof(GameTeam.Domain.AssemblyMarker).Assembly.Should().NotBeNull();
    }
}
