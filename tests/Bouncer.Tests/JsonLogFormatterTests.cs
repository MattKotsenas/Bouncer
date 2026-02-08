using System.Text;
using System.Text.Json;
using Bouncer.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Bouncer.Tests;

[TestClass]
public sealed class JsonLogFormatterTests
{
    [TestMethod]
    public void WritesStructuredJson()
    {
        var formatter = new JsonLogFormatter();
        var scopeProvider = new LoggerExternalScopeProvider();
        using var scope = scopeProvider.Push(new[]
        {
            new KeyValuePair<string, object?>("ScopeKey", "ScopeValue")
        });

        IReadOnlyList<KeyValuePair<string, object?>> state = new List<KeyValuePair<string, object?>>
        {
            new("ToolName", "bash"),
            new("Decision", "Deny"),
            new("{OriginalFormat}", "Audit {ToolName} {Decision}")
        };

        var timestamp = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var entry = new LogEntry<IReadOnlyList<KeyValuePair<string, object?>>>(
            LogLevel.Information,
            "Bouncer.Audit.Deny",
            new EventId(1, "Audit"),
            state,
            new InvalidOperationException("Boom"),
            static (_, __) => "Audit bash",
            timestamp,
            scopeProvider);

        using var stream = new MemoryStream();
        formatter.Write(entry, stream);

        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("timestamp").GetString().Should().Be(timestamp.ToString("O"));
        root.GetProperty("level").GetString().Should().Be("Information");
        root.GetProperty("category").GetString().Should().Be("Bouncer.Audit.Deny");
        root.GetProperty("eventId").GetProperty("id").GetInt32().Should().Be(1);
        root.GetProperty("eventId").GetProperty("name").GetString().Should().Be("Audit");
        root.GetProperty("exception").GetString().Should().Contain("Boom");

        var stateElement = root.GetProperty("state");
        stateElement.GetProperty("ToolName").GetString().Should().Be("bash");
        stateElement.GetProperty("Decision").GetString().Should().Be("Deny");
        stateElement.TryGetProperty("{OriginalFormat}", out _).Should().BeFalse();

        var scopes = root.GetProperty("scopes");
        scopes.GetArrayLength().Should().Be(1);
        scopes[0].GetProperty("ScopeKey").GetString().Should().Be("ScopeValue");
    }
}
