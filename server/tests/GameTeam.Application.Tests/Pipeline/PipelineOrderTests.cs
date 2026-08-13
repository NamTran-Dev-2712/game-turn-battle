using FluentAssertions;
using GameTeam.Application.Tests.TestSupport;
using Xunit;

namespace GameTeam.Application.Tests.Pipeline;

/// <summary>
/// Proves the ACTUAL behavior execution order (not merely the registration call order) by routing
/// probe requests through the real MediatR pipeline with recording collaborators.
/// Order = Logging → Validation → Transaction → Caching.
/// </summary>
public sealed class PipelineOrderTests
{
    [Fact]
    public async Task Transactional_command_runs_logging_validation_transaction_then_handler()
    {
        using TestHost host = TestHost.Create(feedLoggerToRecorder: true);

        await host.Mediator.Send(new ProbeCommand(IsValid: true));

        host.Recorder.Steps.Should().Equal(
            "log:before",
            "validate",
            "tx:begin",
            "handler",
            "tx:commit",
            "log:after");
    }

    [Fact]
    public async Task Cacheable_query_runs_logging_validation_caching_then_handler_and_never_a_transaction()
    {
        using TestHost host = TestHost.Create(feedLoggerToRecorder: true);

        await host.Mediator.Send(new ProbeQuery());

        host.Recorder.Steps.Should().Equal(
            "log:before",
            "validate",
            "cache:get",
            "handler",
            "cache:set",
            "log:after");

        // A query must never enter a command transaction.
        host.Recorder.Steps.Should().NotContain("tx:begin");
    }
}
