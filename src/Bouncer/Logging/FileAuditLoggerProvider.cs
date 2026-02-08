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
        new FileAuditLogger(_options, _sync);

    public void Dispose()
    {
    }

    private sealed class FileAuditLogger : ILogger
    {
        private readonly LoggingOptions _options;
        private readonly object _sync;

        public FileAuditLogger(LoggingOptions options, object sync)
        {
            _options = options;
            _sync = sync;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

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

            if (!TryGetAuditEntry(state, out var entry) || entry is null)
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

        private static bool TryGetAuditEntry<TState>(TState state, out AuditEntry? entry)
        {
            switch (state)
            {
                case AuditEntry auditEntry:
                    entry = auditEntry;
                    return true;
                case IReadOnlyList<KeyValuePair<string, object?>> values:
                    foreach (var value in values)
                    {
                        if ((string.Equals(value.Key, "Entry", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(value.Key, "@Entry", StringComparison.OrdinalIgnoreCase))
                            && value.Value is AuditEntry structuredEntry)
                        {
                            entry = structuredEntry;
                            return true;
                        }
                    }
                    break;
            }

            entry = null;
            return false;
        }
    }

    [JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(AuditEntry))]
    private partial class AuditLogJsonContext : JsonSerializerContext
    {
    }
}
