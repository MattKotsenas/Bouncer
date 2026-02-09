using System.Text.Json;
using Bouncer.Llm;
using Bouncer.Logging;
using Bouncer.Models;
using Bouncer.Options;
using Bouncer.Pipeline;
using Bouncer.Rules;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Bouncer.Tests;

[TestClass]
public sealed class AuditLogTests
{
    [TestMethod]
    public async Task LogsDenials_WhenConfigured()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bouncer-audit-{Guid.NewGuid()}.log");
        var options = new BouncerOptions();
        var loggingOptions = new FileLoggingOptions
        {
            Path = path
        };

        var loggerFactory = CreateLoggerFactory(loggingOptions, LogLevel.None);
        var pipeline = CreatePipeline(options, loggerFactory);

        await pipeline.EvaluateAsync(HookInput.Bash("rm -rf /"));

        var entries = ReadEntries(path);
        entries.Should().ContainSingle();
        var entry = entries.Single();
        entry.GetProperty("category").GetString().Should().Be(AuditLogCategories.Deny);
        entry.GetProperty("level").GetString().Should().Be(LogLevel.Information.ToString());
        entry.GetProperty("state").GetProperty("Decision").GetString()
            .Should().Be(PermissionDecision.Deny.ToString());
    }

    [TestMethod]
    public async Task SkipsAllow_WhenDenialsOnly()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bouncer-audit-{Guid.NewGuid()}.log");
        var options = new BouncerOptions();
        var loggingOptions = new FileLoggingOptions
        {
            Path = path
        };

        var loggerFactory = CreateLoggerFactory(loggingOptions, LogLevel.None);
        var pipeline = CreatePipeline(options, loggerFactory);

        await pipeline.EvaluateAsync(HookInput.Bash("echo ok"));

        File.Exists(path).Should().BeFalse();
    }

    [TestMethod]
    public async Task LogsAllow_WhenAll()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bouncer-audit-{Guid.NewGuid()}.log");
        var options = new BouncerOptions();
        var loggingOptions = new FileLoggingOptions
        {
            Path = path
        };

        var loggerFactory = CreateLoggerFactory(loggingOptions, LogLevel.Information);
        var pipeline = CreatePipeline(options, loggerFactory);

        await pipeline.EvaluateAsync(HookInput.Bash("echo ok"));

        var entries = ReadEntries(path);
        entries.Should().ContainSingle();
        var entry = entries.Single();
        entry.GetProperty("category").GetString().Should().Be(AuditLogCategories.Allow);
        entry.GetProperty("level").GetString().Should().Be(LogLevel.Information.ToString());
        entry.GetProperty("state").GetProperty("Decision").GetString()
            .Should().Be(PermissionDecision.Allow.ToString());
    }

    private static IBouncerPipeline CreatePipeline(BouncerOptions options, ILoggerFactory loggerFactory)
    {
        var optionsWrapper = OptionsFactory.Create(options);
        var engine = new RegexRuleEngine(optionsWrapper, loggerFactory.CreateLogger<RegexRuleEngine>());
        return new BouncerPipeline(engine, new NullLlmJudge(), new HookAdapterFactory([new ClaudeHookAdapter()]), loggerFactory, optionsWrapper);
    }

    private static ILoggerFactory CreateLoggerFactory(FileLoggingOptions options, LogLevel allowLevel) =>
        LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddFilter((category, level) => level >= LogLevel.Error);
            builder.AddFilter(AuditLogCategories.Deny, LogLevel.Information);
            builder.AddFilter(AuditLogCategories.Allow, allowLevel);
            builder.AddProvider(new FileLoggerProvider(options, new JsonLogFormatter()));
        });

    private static List<JsonElement> ReadEntries(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        var entries = new List<JsonElement>();
        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using var document = JsonDocument.Parse(line);
            entries.Add(document.RootElement.Clone());
        }

        File.Delete(path);
        return entries;
    }
}
