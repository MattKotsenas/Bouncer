using Microsoft.Extensions.Logging;

namespace Bouncer.Logging;

public readonly struct LogEntry<TState>
{
    public LogEntry(
        LogLevel logLevel,
        string categoryName,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter,
        DateTimeOffset timestamp,
        IExternalScopeProvider? scopeProvider)
    {
        LogLevel = logLevel;
        CategoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
        EventId = eventId;
        State = state;
        Exception = exception;
        Formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        Timestamp = timestamp;
        ScopeProvider = scopeProvider;
    }

    public LogLevel LogLevel { get; }

    public string CategoryName { get; }

    public EventId EventId { get; }

    public TState State { get; }

    public Exception? Exception { get; }

    public Func<TState, Exception?, string> Formatter { get; }

    public DateTimeOffset Timestamp { get; }

    public IExternalScopeProvider? ScopeProvider { get; }
}
