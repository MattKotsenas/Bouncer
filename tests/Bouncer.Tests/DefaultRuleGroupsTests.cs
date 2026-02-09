using Bouncer.Rules;
using FluentAssertions;

namespace Bouncer.Tests;

[TestClass]
public sealed class DefaultRuleGroupsTests
{
    [TestMethod]
    public void DefaultRuleGroups_ContainAllGroups()
    {
        var groupNames = DefaultRuleGroups.All.Select(group => group.Name);

        groupNames.Should().BeEquivalentTo(
            [
                "bash",
                "powershell",
                "builtins",
                "git",
                "secrets-exposure",
                "production-risk",
                "web"
            ]);
    }

    [TestMethod]
    public void Bash_RulesTargetBashCommands()
    {
        var group = DefaultRuleGroups.All.Single(g => g.Name == "bash");

        group.Rules.Should().AllSatisfy(rule =>
        {
            rule.ToolName.Should().Be("bash");
            rule.Field.Should().Be(ToolField.Command);
        });
    }

    [TestMethod]
    public void Git_IncludesForceWithLease()
    {
        var group = DefaultRuleGroups.All.Single(g => g.Name == "git");
        var forceRule = group.Rules.Single(rule => rule.Name == "git-force-push");

        forceRule.Pattern.Should().Contain("--force-with-lease");
    }

    [TestMethod]
    public void PowerShell_RulesTargetPowerShellCommands()
    {
        var group = DefaultRuleGroups.All.Single(g => g.Name == "powershell");

        group.Rules.Should().AllSatisfy(rule =>
        {
            rule.ToolName.Should().Be("pwsh");
            rule.Field.Should().Be(ToolField.Command);
        });
    }

    [TestMethod]
    public void SecretsExposure_UsesExpectedToolFields()
    {
        var group = DefaultRuleGroups.All.Single(g => g.Name == "secrets-exposure");

        group.Rules
            .Where(rule => rule.ToolName == "bash")
            .Should()
            .AllSatisfy(rule => rule.Field.Should().Be(ToolField.Command));
    }

    [TestMethod]
    public void Builtins_UsesExpectedToolFields()
    {
        var group = DefaultRuleGroups.All.Single(g => g.Name == "builtins");

        group.Rules
            .Where(rule => rule.ToolName is "write" or "edit" or "read")
            .Should()
            .AllSatisfy(rule => rule.Field.Should().Be(ToolField.Path));

        group.Rules
            .Where(rule => rule.ToolName is "glob" or "grep")
            .Should()
            .AllSatisfy(rule => rule.Field.Should().Be(ToolField.Pattern));

        group.Rules
            .Where(rule => rule.ToolName == "todo")
            .Should()
            .AllSatisfy(rule => rule.Field.Should().Be(ToolField.Command));
    }

    [TestMethod]
    public void ProductionRisk_RulesTargetBashCommands()
    {
        var group = DefaultRuleGroups.All.Single(g => g.Name == "production-risk");

        group.Rules.Should().AllSatisfy(rule =>
        {
            rule.ToolName.Should().Be("bash");
            rule.Field.Should().Be(ToolField.Command);
        });
    }

    [TestMethod]
    public void Web_RulesUseUrlOrQuery()
    {
        var group = DefaultRuleGroups.All.Single(g => g.Name == "web");

        group.Rules
            .Where(rule => rule.ToolName == "webfetch")
            .Should()
            .AllSatisfy(rule => rule.Field.Should().Be(ToolField.Url));

        group.Rules
            .Where(rule => rule.ToolName == "websearch")
            .Should()
            .AllSatisfy(rule => rule.Field.Should().Be(ToolField.Query));
    }
}
