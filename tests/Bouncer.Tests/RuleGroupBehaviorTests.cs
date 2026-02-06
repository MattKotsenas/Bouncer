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
        match!.Rule.Name.Should().Be("webfetch-paste");
    }

    [TestMethod]
    public void DangerousGit_DeniesForceWithLease()
    {
        var engine = CreateEngine();

        var match = engine.Evaluate(HookInput.Bash("git push --force-with-lease origin main"));

        match.Should().NotBeNull();
        match!.GroupName.Should().Be("dangerous-git");
    }

    [TestMethod]
    public void ProductionRisk_DeniesProdDelete()
    {
        var engine = CreateEngine();

        var match = engine.Evaluate(HookInput.Bash("curl -X DELETE https://api.prod.example.com/users/1"));

        match.Should().NotBeNull();
        match!.GroupName.Should().Be("production-risk");
    }

    private static RegexRuleEngine CreateEngine() =>
        new(OptionsFactory.Create(new BouncerOptions()));
}
