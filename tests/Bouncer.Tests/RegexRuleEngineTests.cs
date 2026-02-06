using Bouncer.Models;
using Bouncer.Options;
using Bouncer.Rules;
using FluentAssertions;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Bouncer.Tests;

[TestClass]
public sealed class RegexRuleEngineTests
{
    [TestMethod]
    public void MatchesDestructiveShellRules()
    {
        var engine = CreateEngine(new BouncerOptions());

        var match = engine.Evaluate(HookInput.Bash("rm -rf /"));

        match.Should().NotBeNull();
        match!.Decision.Should().Be(PermissionDecision.Deny);
        match.GroupName.Should().Be("destructive-shell");
        match.Rule.Name.Should().Be("rm-rf-root");
    }

    [TestMethod]
    public void DisabledGroup_SkipsMatches()
    {
        var options = new BouncerOptions();
        options.RuleGroups["dangerous-git"].Enabled = false;
        var engine = CreateEngine(options);

        var match = engine.Evaluate(HookInput.Bash("git push --force origin main"));

        match.Should().BeNull();
    }

    [TestMethod]
    public void CustomRules_AreEvaluated()
    {
        var options = new BouncerOptions
        {
            CustomRules =
            [
                new CustomRuleOptions
                {
                    Name = "block-npm-publish",
                    ToolMatch = "bash",
                    Pattern = "npm publish",
                    Action = "deny",
                    Reason = "Publishing blocked - use CI/CD"
                }
            ]
        };

        var engine = CreateEngine(options);

        var match = engine.Evaluate(HookInput.Bash("npm publish"));

        match.Should().NotBeNull();
        match!.Decision.Should().Be(PermissionDecision.Deny);
        match.Reason.Should().Be("Publishing blocked - use CI/CD");
        match.GroupName.Should().Be("custom");
    }

    private static RegexRuleEngine CreateEngine(BouncerOptions options) =>
        new(OptionsFactory.Create(options));
}
