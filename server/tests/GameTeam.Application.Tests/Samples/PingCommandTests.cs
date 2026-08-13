using FluentAssertions;
using GameTeam.Application.Common;
using GameTeam.Application.Features.Diagnostics.Commands;
using GameTeam.Application.Tests.TestSupport;
using GameTeam.Domain.Common;
using Xunit;

namespace GameTeam.Application.Tests.Samples;

/// <summary>End-to-end: the sample <see cref="PingCommand"/> flows through the real MediatR pipeline.</summary>
public sealed class PingCommandTests
{
    [Fact]
    public async Task Valid_command_succeeds_and_commits_through_the_transaction_pipeline()
    {
        using TestHost host = TestHost.Create();

        Result result = await host.Mediator.Send(new PingCommand("hello"));

        result.IsSuccess.Should().BeTrue();
        host.Recorder.Steps.Should().Equal("tx:begin", "tx:commit");
    }

    [Fact]
    public async Task Empty_message_is_rejected_by_validation_before_any_transaction()
    {
        using TestHost host = TestHost.Create();

        Result result = await host.Mediator.Send(new PingCommand(string.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ValidationErrors.Code);
        host.Recorder.Steps.Should().BeEmpty();
    }
}
