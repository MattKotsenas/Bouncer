using Bouncer.Commands;
using Bouncer.Llm;
using Bouncer.Options;
using Bouncer.Pipeline;
using Bouncer.Rules;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Bouncer.Tests;

[TestClass]
public sealed class TestCommandTests
{
    [TestMethod]
    public async Task TestCommand_PrintsBasicOutput()
    {
        var (pipeline, engine, options) = CreateServices();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await TestCommand.ExecuteAsync(
            ["bash", "rm", "-rf", "/"],
            pipeline,
            engine,
            options,
            output,
            error);

        exitCode.Should().Be(2);
        output.ToString().Should().Contain("DENY (Tier 1, regex)");
        error.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public async Task TestCommand_PrintsVerboseOutput()
    {
        var (pipeline, engine, options) = CreateServices();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await TestCommand.ExecuteAsync(
            ["-v", "bash", "rm", "-rf", "/"],
            pipeline,
            engine,
            options,
            output,
            error);

        exitCode.Should().Be(2);
        output.ToString().Should().Contain("Rule:");
        output.ToString().Should().Contain("Pattern:");
        output.ToString().Should().Contain("Time:");
        error.ToString().Should().BeEmpty();
    }

    [TestMethod]
    [DataRow("powershell", "echo hello")]
    [DataRow("pwsh", "echo hello")]
    [DataRow("PowerShell", "echo hello")]
    public async Task TestCommand_AcceptsPowerShellToolType(string tool, string input)
    {
        var (pipeline, engine, options) = CreateServices();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await TestCommand.ExecuteAsync(
            [tool, input],
            pipeline,
            engine,
            options,
            output,
            error);

        exitCode.Should().Be(0);
        error.ToString().Should().BeEmpty();
        output.ToString().Should().Contain("ALLOW");
    }

    [TestMethod]
    public async Task TestCommand_PowerShellDenyIsDetected()
    {
        var (pipeline, engine, options) = CreateServices();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await TestCommand.ExecuteAsync(
            ["powershell", "Remove-Item", "-Recurse", "-Force", @"C:\"],
            pipeline,
            engine,
            options,
            output,
            error);

        exitCode.Should().Be(2);
        output.ToString().Should().Contain("DENY");
    }

    [TestMethod]
    [DataRow("read", "README.md")]
    [DataRow("webfetch", "https://example.com")]
    [DataRow("websearch", "how to test")]
    public async Task TestCommand_AcceptsToolType(string tool, string input)
    {
        var (pipeline, engine, options) = CreateServices();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await TestCommand.ExecuteAsync(
            [tool, input],
            pipeline,
            engine,
            options,
            output,
            error);

        error.ToString().Should().BeEmpty();
        output.ToString().Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task TestCommand_WriteAcceptsPathAndContent()
    {
        var (pipeline, engine, options) = CreateServices();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await TestCommand.ExecuteAsync(
            ["write", "test.txt", "hello"],
            pipeline,
            engine,
            options,
            output,
            error);

        error.ToString().Should().BeEmpty();
        output.ToString().Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task TestCommand_AcceptsArbitraryToolName()
    {
        var (pipeline, engine, options) = CreateServices();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await TestCommand.ExecuteAsync(
            ["terraform", "apply -auto-approve"],
            pipeline,
            engine,
            options,
            output,
            error);

        exitCode.Should().Be(0);
        error.ToString().Should().BeEmpty();
        output.ToString().Should().Contain("ALLOW");
    }

    [TestMethod]
    public async Task TestCommand_MissingArgsReturnsUsage()
    {
        var (pipeline, engine, options) = CreateServices();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await TestCommand.ExecuteAsync(
            ["bash"],
            pipeline,
            engine,
            options,
            output,
            error);

        exitCode.Should().Be(1);
        error.ToString().Should().Contain("Usage:");
    }

    private static (IBouncerPipeline Pipeline, IRuleEngine Engine, BouncerOptions Options) CreateServices()
    {
        var options = new BouncerOptions();
        var optionsWrapper = OptionsFactory.Create(options);
        var loggerFactory = LoggerFactory.Create(builder => { });
        var engine = new RegexRuleEngine(optionsWrapper, loggerFactory.CreateLogger<RegexRuleEngine>());
        var pipeline = new BouncerPipeline(engine, new NullLlmJudge(), new HookAdapterFactory([new ClaudeHookAdapter()]), loggerFactory, optionsWrapper);
        return (pipeline, engine, options);
    }
}
