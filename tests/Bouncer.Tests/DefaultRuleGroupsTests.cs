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
                "destructive-shell",
                "dangerous-git",
                "secrets-exposure",
                "production-risk"
            ]);
    }

    [TestMethod]
    public void DestructiveShell_RulesTargetBashCommands()
    {
        var group = DefaultRuleGroups.All.Single(g => g.Name == "destructive-shell");

        group.Rules.Should().AllSatisfy(rule =>
        {
            rule.ToolName.Should().Be("bash");
            rule.Field.Should().Be(ToolField.Command);
        });
    }

    [TestMethod]
    public void DangerousGit_IncludesForceWithLease()
    {
        var group = DefaultRuleGroups.All.Single(g => g.Name == "dangerous-git");
        var forceRule = group.Rules.Single(rule => rule.Name == "git-force-push");

        forceRule.Pattern.Should().Contain("--force-with-lease");
    }

    [TestMethod]
    public void SecretsExposure_UsesExpectedToolFields()
    {
        var group = DefaultRuleGroups.All.Single(g => g.Name == "secrets-exposure");

        group.Rules
            .Where(rule => rule.ToolName is "write" or "edit" or "read")
            .Should()
            .AllSatisfy(rule => rule.Field.Should().Be(ToolField.Path));

        group.Rules
            .Where(rule => rule.ToolName == "bash")
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
}
