using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GameTeam.Domain.Common;
using Xunit;

namespace GameTeam.Domain.Tests;

public class AggregateRootTests
{
    private sealed record SampleEvent(string Name) : IDomainEvent;

    private sealed class SampleAggregate(int id) : AggregateRoot<int>(id)
    {
        public void DoSomething(string name) => RaiseDomainEvent(new SampleEvent(name));
    }

    [Fact]
    public void New_aggregate_has_no_domain_events()
    {
        new SampleAggregate(1).DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Raising_adds_the_event()
    {
        var aggregate = new SampleAggregate(1);

        aggregate.DoSomething("a");

        aggregate.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SampleEvent>();
    }

    [Fact]
    public void Multiple_events_are_preserved_in_order()
    {
        var aggregate = new SampleAggregate(1);

        aggregate.DoSomething("a");
        aggregate.DoSomething("b");

        aggregate.DomainEvents.Should().HaveCount(2);
        aggregate.DomainEvents.OfType<SampleEvent>().Select(e => e.Name)
            .Should().ContainInOrder("a", "b");
    }

    [Fact]
    public void Clear_removes_all_events()
    {
        var aggregate = new SampleAggregate(1);
        aggregate.DoSomething("a");

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Exposed_collection_cannot_be_mutated_externally()
    {
        var aggregate = new SampleAggregate(1);
        aggregate.DoSomething("a");

        aggregate.DomainEvents.Should().NotBeAssignableTo<List<IDomainEvent>>();

        Action mutate = () =>
            ((ICollection<IDomainEvent>)aggregate.DomainEvents).Add(new SampleEvent("x"));
        mutate.Should().Throw<NotSupportedException>();

        aggregate.DomainEvents.Should().ContainSingle();
    }
}
