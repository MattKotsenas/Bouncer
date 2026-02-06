using Bouncer.Commands;
using Bouncer.Llm;
using Bouncer.Options;
using Bouncer.Pipeline;
using Bouncer.Rules;
using FluentAssertions;
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

    private static (IBouncerPipeline Pipeline, IRuleEngine Engine, BouncerOptions Options) CreateServices()
    {
        var options = new BouncerOptions();
        var optionsWrapper = OptionsFactory.Create(options);
        var engine = new RegexRuleEngine(optionsWrapper);
        var pipeline = new BouncerPipeline(engine, new NullLlmJudge(), optionsWrapper);
        return (pipeline, engine, options);
    }
}
