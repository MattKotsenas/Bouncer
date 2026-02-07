using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bouncer.Options;
using Microsoft.Extensions.Logging;

namespace Bouncer.Logging;

public sealed partial class FileAuditLoggerProvider : ILoggerProvider
{
    private static readonly byte[] NewLine = Encoding.UTF8.GetBytes(Environment.NewLine);
    private readonly LoggingOptions _options;
    private readonly object _sync = new();

    public FileAuditLoggerProvider(LoggingOptions options)
    {
        _options = options;
    }

    public ILogger CreateLogger(string categoryName) =>
        new FileAuditLogger(categoryName, _options, _sync);

    public void Dispose()
    {
    }

    private sealed class FileAuditLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly LoggingOptions _options;
        private readonly object _sync;

        public FileAuditLogger(string categoryName, LoggingOptions options, object sync)
        {
            _categoryName = categoryName;
            _options = options;
            _sync = sync;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel)
        {
            if (string.Equals(_options.Level, "none", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(_options.Level, "denials-only", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(_categoryName, AuditLogCategories.Denials, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return logLevel >= LogLevel.Information;
        }

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

            if (state is not AuditEntry entry)
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

                JsonSerializer.Serialize(stream, entry, AuditLogJsonContext.Default.AuditEntry);
                stream.Write(NewLine, 0, NewLine.Length);
            }
        }
    }

    [JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(AuditEntry))]
    private partial class AuditLogJsonContext : JsonSerializerContext
    {
    }
}
