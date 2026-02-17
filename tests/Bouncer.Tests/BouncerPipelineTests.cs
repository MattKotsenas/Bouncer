using System.Text;
using System.Text.Json;
using Bouncer.Llm;
using Bouncer.Models;
using Bouncer.Options;
using Bouncer.Pipeline;
using Bouncer.Rules;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Bouncer.Tests;

[TestClass]
public sealed class BouncerPipelineTests
{
    [TestMethod]
    public async Task EvaluateAsync_DeniesDangerousCommand()
    {
        var pipeline = CreatePipeline(new BouncerOptions());

        var result = await pipeline.EvaluateAsync(HookInput.Bash("rm -rf /"));

        result.Decision.Should().Be(PermissionDecision.Deny);
        result.Tier.Should().Be(EvaluationTier.Rules);
    }

    [TestMethod]
    public async Task EvaluateAsync_AllowsUnknownCommand_WithDefaultAllow()
    {
        var pipeline = CreatePipeline(new BouncerOptions());

        var result = await pipeline.EvaluateAsync(HookInput.Bash("printf ok"));

        result.Decision.Should().Be(PermissionDecision.Allow);
        result.Tier.Should().Be(EvaluationTier.DefaultAction);
        result.Reason.Should().Contain("defaultAction: allow");
    }

    [TestMethod]
    public async Task EvaluateAsync_DeniesUnknownCommand_WithDefaultDeny()
    {
        var options = new BouncerOptions { DefaultAction = "deny" };
        var pipeline = CreatePipeline(options);

        var result = await pipeline.EvaluateAsync(HookInput.Bash("printf ok"));

        result.Decision.Should().Be(PermissionDecision.Deny);
        result.Tier.Should().Be(EvaluationTier.DefaultAction);
        result.Reason.Should().Contain("defaultAction: deny");
    }

    [TestMethod]
    public async Task EvaluateAsync_UsesLlm_WhenAvailable()
    {
        var options = new BouncerOptions();
        var llmJudge = new FakeLlmJudge(new LlmDecision(PermissionDecision.Deny, "LLM decision", 0.9));
        var pipeline = CreatePipeline(options, llmJudge);

        var result = await pipeline.EvaluateAsync(HookInput.Bash("printf ok"));

        result.Tier.Should().Be(EvaluationTier.Llm);
        result.Decision.Should().Be(PermissionDecision.Deny);
    }

    [TestMethod]
    public async Task RunAsync_WritesHookOutputAndExitCode()
    {
        var pipeline = CreatePipeline(new BouncerOptions());
        var inputJson = JsonSerializer.Serialize(
            HookInput.Bash("rm -rf /"),
            BouncerJsonContext.Default.HookInput);

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(inputJson));
        using var outputStream = new MemoryStream();

        var exitCode = await pipeline.RunAsync(inputStream, outputStream);

        exitCode.Should().Be(2);
        var outputJson = Encoding.UTF8.GetString(outputStream.ToArray());
        outputJson.Should().Contain("\"permissionDecision\":\"deny\"");
    }

    [TestMethod]
    public async Task RunAsync_InvalidJson_UsesDefaultAction()
    {
        var options = new BouncerOptions { DefaultAction = "deny" };
        var pipeline = CreatePipeline(options);

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes("not-json"));
        using var outputStream = new MemoryStream();

        var exitCode = await pipeline.RunAsync(inputStream, outputStream);

        exitCode.Should().Be(2);
        Encoding.UTF8.GetString(outputStream.ToArray()).Should().Contain("\"permissionDecision\":\"deny\"");
    }

    [TestMethod]
    public async Task RunAsync_CopilotFormat_DeniesRmRf()
    {
        var pipeline = CreatePipeline(new BouncerOptions());
        var json = """{"toolName":"bash","toolArgs":"{\"command\":\"rm -rf /\"}","cwd":"/tmp"}""";

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var outputStream = new MemoryStream();

        var exitCode = await pipeline.RunAsync(inputStream, outputStream);

        exitCode.Should().Be(2);
        var output = Encoding.UTF8.GetString(outputStream.ToArray());
        output.Should().Contain("\"permissionDecision\":\"deny\"");
        output.Should().NotContain("hookSpecificOutput");
    }

    [TestMethod]
    public async Task RunAsync_CopilotFormat_AllowsSafeCommand()
    {
        var pipeline = CreatePipeline(new BouncerOptions());
        var json = """{"toolName":"bash","toolArgs":"{\"command\":\"echo hello\"}","cwd":"/tmp"}""";

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var outputStream = new MemoryStream();

        var exitCode = await pipeline.RunAsync(inputStream, outputStream);

        exitCode.Should().Be(0);
        var output = Encoding.UTF8.GetString(outputStream.ToArray());
        output.Should().Contain("\"permissionDecision\":\"allow\"");
        output.Should().NotContain("hookSpecificOutput");
    }

    [TestMethod]
    public async Task RunAsync_ClaudeFormat_WrapsInHookSpecificOutput()
    {
        var pipeline = CreatePipeline(new BouncerOptions());
        var json = """{"tool_name":"Bash","tool_input":{"command":"rm -rf /"},"cwd":"/tmp"}""";

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var outputStream = new MemoryStream();

        var exitCode = await pipeline.RunAsync(inputStream, outputStream);

        exitCode.Should().Be(2);
        Encoding.UTF8.GetString(outputStream.ToArray()).Should().Contain("\"hookSpecificOutput\"");
    }

    [TestMethod]
    public async Task RunAsync_InvalidJson_LogsWarning()
    {
        var options = new BouncerOptions { DefaultAction = "allow" };
        var collector = new FakeLogCollector();
        var factory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning).AddProvider(new FakeLoggerProvider(collector)));
        var pipeline = CreatePipeline(options, loggerFactory: factory);

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes("not-json"));
        using var outputStream = new MemoryStream();

        await pipeline.RunAsync(inputStream, outputStream);

        collector.GetSnapshot().Should().ContainSingle(r => r.Message.Contains("Failed to parse hook input"));
    }

    [TestMethod]
    public async Task RunAsync_UnknownToolArgs_AuditLogContainsExtensionData()
    {
        var options = new BouncerOptions { DefaultAction = "allow" };
        var collector = new FakeLogCollector();
        var factory = LoggerFactory.Create(b =>
            b.SetMinimumLevel(LogLevel.Information).AddProvider(new FakeLoggerProvider(collector)));
        var pipeline = CreatePipeline(options, loggerFactory: factory);

        // Copilot CLI format with unknown tool args (the bug scenario)
        var json = """{"toolName":"ledger_append","toolArgs":"{\"request\":{\"plan_id\":\"fix-typos\"}}","cwd":"/tmp"}""";
        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var outputStream = new MemoryStream();

        await pipeline.RunAsync(inputStream, outputStream);

        var auditLog = collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("Audit") && r.Message.Contains("ledger_append"));
        auditLog.Should().NotBeNull("audit log entry should be written for the tool call");
        auditLog!.Message.Should().Contain("fix-typos", "extension data should be included in the audit log");
    }

    private static IBouncerPipeline CreatePipeline(
        BouncerOptions options,
        ILlmJudge? llmJudge = null,
        ILoggerFactory? loggerFactory = null)
    {
        var optionsWrapper = OptionsFactory.Create(options);
        var factory = loggerFactory ?? LoggerFactory.Create(builder => { });
        var engine = new RegexRuleEngine(optionsWrapper, factory.CreateLogger<RegexRuleEngine>());
        var adapterFactory = new HookAdapterFactory([new ClaudeHookAdapter(), new CopilotHookAdapter()]);
        return new BouncerPipeline(
            engine,
            llmJudge ?? new NullLlmJudge(),
            adapterFactory,
            factory,
            optionsWrapper);
    }

    private sealed class FakeLlmJudge : ILlmJudge
    {
        private readonly LlmDecision _decision;

        public FakeLlmJudge(LlmDecision decision)
        {
            _decision = decision;
        }

        public Task<LlmDecision?> EvaluateAsync(HookInput input, CancellationToken cancellationToken = default) =>
            Task.FromResult<LlmDecision?>(_decision);
    }
}
