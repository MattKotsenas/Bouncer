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
        var options = new BouncerOptions
        {
            Logging = new LoggingOptions
            {
                Level = "denials-only",
                Path = path
            }
        };

        var loggerFactory = CreateLoggerFactory(options);
        var pipeline = CreatePipeline(options, loggerFactory);

        await pipeline.EvaluateAsync(HookInput.Bash("rm -rf /"));

        var entries = ReadEntries(path);
        entries.Should().ContainSingle(entry => entry.Decision == PermissionDecision.Deny);
    }

    [TestMethod]
    public async Task SkipsAllow_WhenDenialsOnly()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bouncer-audit-{Guid.NewGuid()}.log");
        var options = new BouncerOptions
        {
            Logging = new LoggingOptions
            {
                Level = "denials-only",
                Path = path
            }
        };

        var loggerFactory = CreateLoggerFactory(options);
        var pipeline = CreatePipeline(options, loggerFactory);

        await pipeline.EvaluateAsync(HookInput.Bash("echo ok"));

        File.Exists(path).Should().BeFalse();
    }

    [TestMethod]
    public async Task LogsAllow_WhenAll()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bouncer-audit-{Guid.NewGuid()}.log");
        var options = new BouncerOptions
        {
            Logging = new LoggingOptions
            {
                Level = "all",
                Path = path
            }
        };

        var loggerFactory = CreateLoggerFactory(options);
        var pipeline = CreatePipeline(options, loggerFactory);

        await pipeline.EvaluateAsync(HookInput.Bash("echo ok"));

        var entries = ReadEntries(path);
        entries.Should().ContainSingle(entry => entry.Decision == PermissionDecision.Allow);
    }

    private static IBouncerPipeline CreatePipeline(BouncerOptions options, ILoggerFactory loggerFactory)
    {
        var optionsWrapper = OptionsFactory.Create(options);
        var engine = new RegexRuleEngine(optionsWrapper);
        return new BouncerPipeline(engine, new NullLlmJudge(), loggerFactory, optionsWrapper);
    }

    private static ILoggerFactory CreateLoggerFactory(BouncerOptions options) =>
        LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new FileAuditLoggerProvider(options.Logging));
        });

    private static List<AuditEntry> ReadEntries(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        var entries = File.ReadAllLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<AuditEntry>(line))
            .Where(entry => entry is not null)
            .Select(entry => entry!)
            .ToList();

        File.Delete(path);
        return entries;
    }
}
