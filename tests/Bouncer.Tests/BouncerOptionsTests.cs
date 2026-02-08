using Bouncer.Options;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Bouncer.Tests;

[TestClass]
public sealed class BouncerOptionsTests
{
    [TestMethod]
    public void Defaults_AreCorrect()
    {
        var options = new BouncerOptions();

        options.Version.Should().Be(1);
        options.DefaultAction.Should().Be("allow");
        options.RuleGroups.Should().ContainKeys(
            "bash",
            "git",
            "secrets-exposure",
            "production-risk",
            "web");

        options.RuleGroups.Values.Should().AllSatisfy(group =>
        {
            group.Enabled.Should().BeTrue();
        });
        options.CustomRules.Should().BeEmpty();
        options.LlmFallback.Enabled.Should().BeTrue();
        options.LlmFallback.ProviderChain.Should().NotBeEmpty();
        options.Logging.Path.Should().Be(".bouncer/audit.log");
    }

    [TestMethod]
    public void Validate_Defaults_Succeeds()
    {
        var validator = new BouncerOptionsValidator();

        var result = validator.Validate(null, new BouncerOptions());

        result.Succeeded.Should().BeTrue();
    }

    [TestMethod]
    public void Validate_InvalidDefaultAction_Fails()
    {
        var validator = new BouncerOptionsValidator();
        var options = new BouncerOptions { DefaultAction = "block" };

        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("defaultAction");
    }

    [TestMethod]
    public void Validate_EmptyProviderChain_WhenEnabled_Fails()
    {
        var validator = new BouncerOptionsValidator();
        var options = new BouncerOptions();
        options.LlmFallback.ProviderChain.Clear();

        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("providerChain");
    }
}
