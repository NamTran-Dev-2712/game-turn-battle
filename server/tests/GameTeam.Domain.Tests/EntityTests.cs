using System;
using FluentAssertions;
using GameTeam.Domain.Common;
using Xunit;

namespace GameTeam.Domain.Tests;

public class EntityTests
{
    private sealed class SampleEntity(int id) : Entity<int>(id);

    private sealed class OtherEntity(int id) : Entity<int>(id);

    private sealed class StringEntity(string id) : Entity<string>(id);

    [Fact]
    public void Same_id_same_type_are_equal_with_consistent_hash()
    {
        var a = new SampleEntity(1);
        var b = new SampleEntity(1);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Different_id_is_not_equal()
    {
        new SampleEntity(1).Should().NotBe(new SampleEntity(2));
        (new SampleEntity(1) != new SampleEntity(2)).Should().BeTrue();
    }

    [Fact]
    public void Same_id_different_type_is_not_equal()
    {
        Entity<int> a = new SampleEntity(1);
        Entity<int> b = new OtherEntity(1);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Null_is_not_equal()
    {
        var a = new SampleEntity(1);

        a.Equals(null).Should().BeFalse();
        (a == null).Should().BeFalse();
        (null == a).Should().BeFalse();
    }

    [Fact]
    public void Null_id_throws()
    {
        Action act = () => _ = new StringEntity(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
