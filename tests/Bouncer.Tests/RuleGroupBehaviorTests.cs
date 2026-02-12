using Bouncer.Models;
using Bouncer.Options;
using Bouncer.Rules;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Bouncer.Tests;

[TestClass]
public sealed class RuleGroupBehaviorTests
{
    [TestMethod]
    public void Builtins_DeniesEnvWrite()
    {
        var engine = CreateEngine();

        var match = engine.Evaluate(HookInput.Write("C:\\project\\.env", "secret=value"));

        match.Should().NotBeNull();
        match!.GroupName.Should().Be("builtins");
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
    public void Builtins_AllowsReportIntent()
    {
        var engine = CreateEngine();
        var input = new HookInput
        {
            ToolName = "report_intent",
            ToolInput = new ToolInput()
        };

        var match = engine.Evaluate(input);

        match.Should().NotBeNull();
        match!.Decision.Should().Be(PermissionDecision.Allow);
        match.GroupName.Should().Be("builtins");
    }

    [TestMethod]
    public void Builtins_AllowsRipgrep()
    {
        var engine = CreateEngine();
        var input = new HookInput
        {
            ToolName = "rg",
            ToolInput = ToolInput.ForPathAndPattern("src", "TODO")
        };

        var match = engine.Evaluate(input);

        match.Should().NotBeNull();
        match!.Decision.Should().Be(PermissionDecision.Allow);
        match.GroupName.Should().Be("builtins");
        match.Rule.Name.Should().Be("safe-grep");
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
    public void Bash_SafeCommand_DoesNotAllowNewline()
    {
        var engine = CreateEngine();

        var match = engine.Evaluate(HookInput.Bash("ls\npwd"));

        match.Should().BeNull();
    }

    [TestMethod]
    public void Git_SafeCommand_DoesNotAllowNewline()
    {
        var engine = CreateEngine();

        var match = engine.Evaluate(HookInput.Bash("git status\npwd"));

        match.Should().BeNull();
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

    [TestMethod]
    public void PowerShell_SafeCommand_DoesNotAllowNewline()
    {
        var engine = CreateEngine();
        var input = new HookInput
        {
            ToolName = "powershell",
            ToolInput = ToolInput.ForCommand("Get-ChildItem\nGet-Process")
        };

        var match = engine.Evaluate(input);

        match.Should().BeNull();
    }

    [TestMethod]
    public void ProductionRisk_KubectlApplyWithDryRun_DoesNotMatch()
    {
        var engine = CreateEngine();

        var match = engine.Evaluate(HookInput.Bash("kubectl apply -f prod.yaml --dry-run=client"));

        match.Should().BeNull();
    }

    [TestMethod]
    public void ProductionRisk_KubectlApplyWithDryRunInComment_Denies()
    {
        var engine = CreateEngine();

        var match = engine.Evaluate(HookInput.Bash("kubectl apply -f prod.yaml # --dry-run=client"));

        match.Should().NotBeNull();
        match!.Decision.Should().Be(PermissionDecision.Deny);
        match.GroupName.Should().Be("production-risk");
    }

    [TestMethod]
    public void Builtins_BlockBouncerConfigEdit()
    {
        var engine = CreateEngine();

        var match = engine.Evaluate(HookInput.Edit(".bouncer/config.json", "{ }"));

        match.Should().NotBeNull();
        match!.Decision.Should().Be(PermissionDecision.Deny);
        match.GroupName.Should().Be("builtins");
    }

    [TestMethod]
    public void Builtins_DeniesApplyPatchToSecretFile()
    {
        var engine = CreateEngine();
        var input = new HookInput
        {
            ToolName = "apply_patch",
            ToolInput = ToolInput.ForPath("/home/user/.env")
        };

        var match = engine.Evaluate(input);

        match.Should().NotBeNull();
        match!.Decision.Should().Be(PermissionDecision.Deny);
        match.GroupName.Should().Be("builtins");
    }

    private static RegexRuleEngine CreateEngine() =>
        new(OptionsFactory.Create(new BouncerOptions()), LoggerFactory.Create(builder => { }).CreateLogger<RegexRuleEngine>());
}
