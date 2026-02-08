using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Bouncer.Logging;

public sealed class JsonLogFormatter : ILogFormatter
{
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        SkipValidation = true
    };

    public void Write<TState>(in LogEntry<TState> logEntry, Stream stream)
    {
        using var writer = new Utf8JsonWriter(stream, WriterOptions);
        writer.WriteStartObject();

        writer.WriteString("timestamp", logEntry.Timestamp.ToString("O", CultureInfo.InvariantCulture));
        writer.WriteString("level", logEntry.LogLevel.ToString());
        writer.WriteString("category", logEntry.CategoryName);
        WriteEventId(writer, logEntry.EventId);

        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        writer.WriteString("message", message);

        if (logEntry.Exception is not null)
        {
            writer.WriteString("exception", logEntry.Exception.ToString());
        }

        WriteState(writer, logEntry.State);
        WriteScopes(writer, logEntry.ScopeProvider);

        writer.WriteEndObject();
        writer.Flush();
    }

    private static void WriteEventId(Utf8JsonWriter writer, EventId eventId)
    {
        writer.WritePropertyName("eventId");
        writer.WriteStartObject();
        writer.WriteNumber("id", eventId.Id);
        if (!string.IsNullOrWhiteSpace(eventId.Name))
        {
            writer.WriteString("name", eventId.Name);
        }
        writer.WriteEndObject();
    }

    private static void WriteState<TState>(Utf8JsonWriter writer, TState state)
    {
        if (state is null)
        {
            return;
        }

        if (state is IEnumerable<KeyValuePair<string, object?>> values)
        {
            writer.WritePropertyName("state");
            writer.WriteStartObject();
            foreach (var value in values)
            {
                if (string.Equals(value.Key, "{OriginalFormat}", StringComparison.Ordinal))
                {
                    continue;
                }

                writer.WritePropertyName(value.Key);
                WriteValue(writer, value.Value);
            }
            writer.WriteEndObject();
            return;
        }

        writer.WritePropertyName("state");
        WriteValue(writer, state);
    }

    private static void WriteScopes(Utf8JsonWriter writer, IExternalScopeProvider? scopeProvider)
    {
        if (scopeProvider is null)
        {
            return;
        }

        var scopes = new List<object?>();
        scopeProvider.ForEachScope(static (scope, state) => state.Add(scope), scopes);

        if (scopes.Count == 0)
        {
            return;
        }

        writer.WritePropertyName("scopes");
        writer.WriteStartArray();
        foreach (var scope in scopes)
        {
            if (scope is IEnumerable<KeyValuePair<string, object?>> values)
            {
                writer.WriteStartObject();
                foreach (var value in values)
                {
                    if (string.Equals(value.Key, "{OriginalFormat}", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    writer.WritePropertyName(value.Key);
                    WriteValue(writer, value.Value);
                }
                writer.WriteEndObject();
            }
            else
            {
                WriteValue(writer, scope);
            }
        }
        writer.WriteEndArray();
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                return;
            case string text:
                writer.WriteStringValue(text);
                return;
            case bool boolean:
                writer.WriteBooleanValue(boolean);
                return;
            case int intValue:
                writer.WriteNumberValue(intValue);
                return;
            case long longValue:
                writer.WriteNumberValue(longValue);
                return;
            case double doubleValue:
                writer.WriteNumberValue(doubleValue);
                return;
            case float floatValue:
                writer.WriteNumberValue(floatValue);
                return;
            case decimal decimalValue:
                writer.WriteNumberValue(decimalValue);
                return;
            case DateTime dateTime:
                writer.WriteStringValue(dateTime.ToString("O", CultureInfo.InvariantCulture));
                return;
            case DateTimeOffset dateTimeOffset:
                writer.WriteStringValue(dateTimeOffset.ToString("O", CultureInfo.InvariantCulture));
                return;
            case Guid guid:
                writer.WriteStringValue(guid.ToString());
                return;
            case Enum enumValue:
                writer.WriteStringValue(enumValue.ToString());
                return;
        }

        writer.WriteStringValue(value.ToString() ?? string.Empty);
    }
}
