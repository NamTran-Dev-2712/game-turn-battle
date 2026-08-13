using FluentAssertions;
using GameTeam.Application.Features.Diagnostics.Commands;
using GameTeam.Application.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameTeam.Application.Tests.Behaviors;

/// <summary>
/// LoggingBehavior: logs request name + elapsed time + outcome for both commands and queries, and
/// never dumps the request payload (no sensitive-data leak).
/// </summary>
public sealed class LoggingBehaviorTests
{
    private const string Secret = "super-secret-token-value";

    [Fact]
    public async Task Command_logs_request_name_elapsed_and_outcome()
    {
        using TestHost host = TestHost.Create();

        await host.Mediator.Send(new PingCommand(Secret));

        IReadOnlyCollection<LogEntry> entries = host.Logger.Entries;

        entries.Should().Contain(e => e.Message.Contains("PingCommand"));
        entries.Should().Contain(e =>
            e.Level == LogLevel.Information
            && e.Message.Contains("ms")
            && e.Message.Contains("Success"));
    }

    [Fact]
    public async Task Does_not_log_sensitive_request_payload()
    {
        using TestHost host = TestHost.Create();

        await host.Mediator.Send(new PingCommand(Secret));

        host.Logger.Entries.Should().NotContain(e => e.Message.Contains(Secret));
    }

    [Fact]
    public async Task Query_is_also_logged()
    {
        using TestHost host = TestHost.Create();

        await host.Mediator.Send(new ProbeQuery());

        host.Logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Information && e.Message.Contains("ProbeQuery"));
    }
}
