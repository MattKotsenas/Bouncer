using Bouncer.Llm;
using Bouncer.Logging;
using Bouncer.Models;
using Bouncer.Options;
using Bouncer.Pipeline;
using Bouncer.Rules;
using FluentAssertions;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Bouncer.Tests;

[TestClass]
public sealed class AuditLogTests
{
    [TestMethod]
    public async Task LogsDenials_WhenConfigured()
    {
        var options = new BouncerOptions
        {
            Logging = new LoggingOptions
            {
                Enabled = true,
                Level = "denials-only"
            }
        };

        var auditLog = new InMemoryAuditLog();
        var pipeline = CreatePipeline(options, auditLog);

        await pipeline.EvaluateAsync(HookInput.Bash("rm -rf /"));

        auditLog.Entries.Should().ContainSingle(entry => entry.Decision == PermissionDecision.Deny);
    }

    [TestMethod]
    public async Task SkipsAllow_WhenDenialsOnly()
    {
        var options = new BouncerOptions
        {
            Logging = new LoggingOptions
            {
                Enabled = true,
                Level = "denials-only"
            }
        };

        var auditLog = new InMemoryAuditLog();
        var pipeline = CreatePipeline(options, auditLog);

        await pipeline.EvaluateAsync(HookInput.Bash("echo ok"));

        auditLog.Entries.Should().BeEmpty();
    }

    private static IBouncerPipeline CreatePipeline(BouncerOptions options, IAuditLog auditLog)
    {
        var optionsWrapper = OptionsFactory.Create(options);
        var engine = new RegexRuleEngine(optionsWrapper);
        return new BouncerPipeline(engine, new NullLlmJudge(), auditLog, optionsWrapper);
    }

    private sealed class InMemoryAuditLog : IAuditLog
    {
        public List<AuditEntry> Entries { get; } = [];

        public Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }
}
