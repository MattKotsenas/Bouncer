using Bouncer.Models;
using Bouncer.Options;
using Bouncer.Rules;
using FluentAssertions;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Bouncer.Tests;

[TestClass]
public sealed class RuleGroupBehaviorTests
{
    [TestMethod]
    public void SecretsExposure_DeniesEnvWrite()
    {
        var engine = CreateEngine();

        var match = engine.Evaluate(HookInput.Write("C:\\project\\.env", "secret=value"));

        match.Should().NotBeNull();
        match!.GroupName.Should().Be("secrets-exposure");
    }

    [TestMethod]
    public void SecretsExposure_DeniesPasteWebFetch()
    {
        var engine = CreateEngine();

        var match = engine.Evaluate(HookInput.WebFetch("https://pastebin.com/raw/abc123"));

        match.Should().NotBeNull();
        match!.GroupName.Should().Be("web");
    }

    [TestMethod]
    public void DangerousGit_DeniesForceWithLease()
    {
        var engine = CreateEngine();

        var match = engine.Evaluate(HookInput.Bash("git push --force-with-lease origin main"));

        match.Should().NotBeNull();
        match!.GroupName.Should().Be("git");
    }

    [TestMethod]
    public void ProductionRisk_DeniesProdDelete()
    {
        var engine = CreateEngine();

        var match = engine.Evaluate(HookInput.Bash("curl -X DELETE https://api.prod.example.com/users/1"));

        match.Should().NotBeNull();
        match!.GroupName.Should().Be("production-risk");
    }

    [TestMethod]
    public void Bash_AllowsSafeCommand()
    {
        var engine = CreateEngine();

        var match = engine.Evaluate(HookInput.Bash("ls -la"));

        match.Should().NotBeNull();
        match!.Decision.Should().Be(PermissionDecision.Allow);
        match.GroupName.Should().Be("bash");
    }

    [TestMethod]
    public void PowerShell_DeniesDestructiveRemove()
    {
        var engine = CreateEngine();
        var input = new HookInput
        {
            ToolName = "powershell",
            ToolInput = ToolInput.ForCommand("Remove-Item -Recurse -Force C:\\")
        };

        var match = engine.Evaluate(input);

        match.Should().NotBeNull();
        match!.Decision.Should().Be(PermissionDecision.Deny);
        match.GroupName.Should().Be("powershell");
    }

    private static RegexRuleEngine CreateEngine() =>
        new(OptionsFactory.Create(new BouncerOptions()));
}
