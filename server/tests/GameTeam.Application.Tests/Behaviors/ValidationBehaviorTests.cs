using FluentAssertions;
using GameTeam.Application.Common;
using GameTeam.Application.Tests.TestSupport;
using GameTeam.Domain.Common;
using Xunit;

namespace GameTeam.Application.Tests.Behaviors;

/// <summary>
/// ValidationBehavior: invalid request → failed <see cref="Result"/> (standard error), the handler
/// does NOT run, and no validation exception escapes. Verified through the real MediatR pipeline.
/// </summary>
public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Invalid_request_returns_failure_and_does_not_execute_handler()
    {
        using TestHost host = TestHost.Create();

        Result result = await host.Mediator.Send(new ProbeCommand(IsValid: false));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ValidationErrors.Code);
        host.Recorder.Steps.Should().Contain("validate");
        host.Recorder.Steps.Should().NotContain("handler");
    }

    [Fact]
    public async Task Invalid_request_does_not_throw()
    {
        using TestHost host = TestHost.Create();

        Func<Task> act = () => host.Mediator.Send(new ProbeCommand(IsValid: false));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Valid_request_reaches_handler_and_succeeds()
    {
        using TestHost host = TestHost.Create();

        Result result = await host.Mediator.Send(new ProbeCommand(IsValid: true));

        result.IsSuccess.Should().BeTrue();
        host.Recorder.Steps.Should().Contain("handler");
    }

    [Fact]
    public async Task Validation_failure_preserves_error_information()
    {
        using TestHost host = TestHost.Create();

        Result result = await host.Mediator.Send(new ProbeCommand(IsValid: false));

        result.Error.Message.Should().Contain("IsValid must be true.");
    }
}
