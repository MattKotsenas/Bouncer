using System.Text;
using Bouncer.Options;
using Microsoft.Extensions.Logging;

namespace Bouncer.Logging;

public sealed class FileLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private static readonly byte[] NewLine = Encoding.UTF8.GetBytes(Environment.NewLine);
    private readonly LoggingOptions _options;
    private readonly ILogFormatter _formatter;
    private readonly object _sync = new();
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public FileLoggerProvider(LoggingOptions options, ILogFormatter formatter)
    {
        _options = options;
        _formatter = formatter;
    }

    public ILogger CreateLogger(string categoryName) =>
        new FileLogger(categoryName, _options, _formatter, _sync, () => _scopeProvider);

    public void Dispose()
    {
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider ?? new LoggerExternalScopeProvider();
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly LoggingOptions _options;
        private readonly ILogFormatter _formatter;
        private readonly object _sync;
        private readonly Func<IExternalScopeProvider> _scopeProviderAccessor;

        public FileLogger(
            string categoryName,
            LoggingOptions options,
            ILogFormatter formatter,
            object sync,
            Func<IExternalScopeProvider> scopeProviderAccessor)
        {
            _categoryName = categoryName;
            _options = options;
            _formatter = formatter;
            _sync = sync;
            _scopeProviderAccessor = scopeProviderAccessor;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            _scopeProviderAccessor().Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.Path))
            {
                return;
            }

            lock (_sync)
            {
                var directory = Path.GetDirectoryName(_options.Path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var stream = new FileStream(
                    _options.Path,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.Read);

                var entry = new LogEntry<TState>(
                    logLevel,
                    _categoryName,
                    eventId,
                    state,
                    exception,
                    formatter,
                    DateTimeOffset.UtcNow,
                    _scopeProviderAccessor());
                _formatter.Write(entry, stream);
                stream.Write(NewLine, 0, NewLine.Length);
            }
        }
    }
}
