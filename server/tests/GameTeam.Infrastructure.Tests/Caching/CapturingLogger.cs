using Microsoft.Extensions.Logging;

namespace GameTeam.Infrastructure.Tests.Caching;

/// <summary>Logger test ghi lại (level, message) để khẳng định có cảnh báo khi Redis degrade.</summary>
public sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly List<(LogLevel Level, string Message)> _entries = new();

    public IReadOnlyList<(LogLevel Level, string Message)> Entries => _entries;

    public bool HasWarning => _entries.Any(e => e.Level == LogLevel.Warning);

    IDisposable? ILogger.BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => _entries.Add((logLevel, formatter(state, exception)));
}
