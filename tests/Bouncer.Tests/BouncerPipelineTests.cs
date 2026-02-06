using System.Text;
using System.Text.Json;
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

        var result = await pipeline.EvaluateAsync(HookInput.Bash("echo ok"));

        result.Decision.Should().Be(PermissionDecision.Allow);
        result.Tier.Should().Be(EvaluationTier.DefaultAction);
        result.Reason.Should().Contain("defaultAction: allow");
    }

    [TestMethod]
    public async Task EvaluateAsync_DeniesUnknownCommand_WithDefaultDeny()
    {
        var options = new BouncerOptions { DefaultAction = "deny" };
        var pipeline = CreatePipeline(options);

        var result = await pipeline.EvaluateAsync(HookInput.Bash("echo ok"));

        result.Decision.Should().Be(PermissionDecision.Deny);
        result.Tier.Should().Be(EvaluationTier.DefaultAction);
        result.Reason.Should().Contain("defaultAction: deny");
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

    private static IBouncerPipeline CreatePipeline(BouncerOptions options)
    {
        var optionsWrapper = OptionsFactory.Create(options);
        var engine = new RegexRuleEngine(optionsWrapper);
        return new BouncerPipeline(engine, new NullLlmJudge(), new NullAuditLog(), optionsWrapper);
    }
}
