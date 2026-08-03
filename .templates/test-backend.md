# Template: Backend Test (xUnit)

Guide for a backend test. Stack: xUnit + FluentAssertions + NSubstitute (+ NetArchTest for
boundaries). See `docs/testing/backend-testing.md`.

```csharp
public sealed class DoThingCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidInput_Succeeds()
    {
        // Arrange — substitute ports; inject a fixed IClock (never real time).
        // var clock = Substitute.For<IClock>();
        // var sut = new DoThingCommandHandler(/* ports */);

        // Act
        // var result = await sut.Handle(new DoThingCommand(1), CancellationToken.None);

        // Assert — behavior, not internals.
        // result.Success.Should().BeTrue();
    }
}
```

Rules: test behavior not internals; deterministic (inject clock/seeded RNG); cover risk logic
(combat/economy/save) first; don't reduce coverage in risk areas.
