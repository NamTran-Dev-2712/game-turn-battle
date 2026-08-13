using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace GameTeam.Application.Tests.TestSupport;

/// <summary>A single captured log entry.</summary>
public sealed record LogEntry(LogLevel Level, string Category, string Message);

/// <summary>
/// In-memory <see cref="ILoggerProvider"/> capturing every formatted log message. Optionally forwards
/// LoggingBehavior's start/end markers ("Handling"/"Handled") into an <see cref="ExecutionRecorder"/>
/// so pipeline order can include the logging boundary.
/// </summary>
public sealed class RecordingLoggerProvider(ExecutionRecorder? recorder = null) : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyCollection<LogEntry> Entries => _entries.ToArray();

    public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, this);

    public void Dispose()
    {
    }

    private void Capture(LogEntry entry)
    {
        _entries.Enqueue(entry);

        if (recorder is null)
        {
            return;
        }

        if (entry.Message.StartsWith("Handling ", StringComparison.Ordinal))
        {
            recorder.Add("log:before");
        }
        else if (entry.Message.StartsWith("Handled ", StringComparison.Ordinal))
        {
            recorder.Add("log:after");
        }
    }

    private sealed class RecordingLogger(string category, RecordingLoggerProvider owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => owner.Capture(new LogEntry(logLevel, category, formatter(state, exception)));
    }
}
