using System.Text.Json;
using Bouncer.Models;
using FluentAssertions;

namespace Bouncer.Tests;

[TestClass]
public sealed class HookInputTests
{
    [TestMethod]
    [DynamicData(nameof(FactoryCases))]
    public void HookInputFactories_RoundTrip(FactoryCase testCase)
    {
        var input = testCase.Input;

        input.ToolName.Should().Be(testCase.ToolName);
        AssertToolInput(input.ToolInput, testCase);

        var json = JsonSerializer.Serialize(input, BouncerJsonContext.Default.HookInput);
        var roundTrip = JsonSerializer.Deserialize(json, BouncerJsonContext.Default.HookInput);

        roundTrip.Should().NotBeNull();
        roundTrip!.ToolName.Should().Be(testCase.ToolName);
        AssertToolInput(roundTrip.ToolInput, testCase);
    }

    [TestMethod]
    public void HookOutputDeny_SerializesExpectedFields()
    {
        var output = HookOutput.Deny("blocked: dangerous command");

        var json = JsonSerializer.Serialize(output, BouncerJsonContext.Default.HookOutput);

        json.Should().Contain("\"permissionDecision\":\"deny\"");
        json.Should().Contain("\"permissionDecisionReason\":\"blocked: dangerous command\"");
    }

    private static IEnumerable<object[]> FactoryCases()
    {
        yield return new object[]
        {
            new FactoryCase("bash", HookInput.Bash("rm -rf /"), Command: "rm -rf /")
        };

        yield return new object[]
        {
            new FactoryCase(
                "write",
                HookInput.Write("C:\\temp\\note.txt", "content"),
                Path: "C:\\temp\\note.txt",
                Content: "content")
        };

        yield return new object[]
        {
            new FactoryCase(
                "edit",
                HookInput.Edit("C:\\temp\\note.txt", "updated"),
                Path: "C:\\temp\\note.txt",
                Content: "updated")
        };

        yield return new object[]
        {
            new FactoryCase("read", HookInput.Read("C:\\temp\\note.txt"), Path: "C:\\temp\\note.txt")
        };

        yield return new object[]
        {
            new FactoryCase(
                "glob",
                HookInput.Glob("C:\\temp", "*.txt"),
                Path: "C:\\temp",
                Pattern: "*.txt")
        };

        yield return new object[]
        {
            new FactoryCase(
                "grep",
                HookInput.Grep("C:\\temp", "password"),
                Path: "C:\\temp",
                Pattern: "password")
        };

        yield return new object[]
        {
            new FactoryCase("web_fetch", HookInput.WebFetch("https://example.com"), Url: "https://example.com")
        };

        yield return new object[]
        {
            new FactoryCase("web_search", HookInput.WebSearch("bouncer hooks"), Query: "bouncer hooks")
        };

        var arguments = new Dictionary<string, JsonElement>
        {
            ["query"] = JsonSerializer.SerializeToElement("select 1")
        };

        yield return new object[]
        {
            new FactoryCase(
                "mcp.database.query",
                HookInput.Mcp("mcp.database.query", arguments),
                ArgumentKey: "query")
        };
    }

    private static void AssertToolInput(ToolInput toolInput, FactoryCase testCase)
    {
        toolInput.Command.Should().Be(testCase.Command);
        toolInput.Path.Should().Be(testCase.Path);
        toolInput.Content.Should().Be(testCase.Content);
        toolInput.Pattern.Should().Be(testCase.Pattern);
        toolInput.Query.Should().Be(testCase.Query);
        toolInput.Url.Should().Be(testCase.Url);

        if (testCase.ArgumentKey is null)
        {
            toolInput.Arguments.Should().BeNull();
        }
        else
        {
            toolInput.Arguments.Should().ContainKey(testCase.ArgumentKey);
        }
    }

    public sealed record FactoryCase(
        string ToolName,
        HookInput Input,
        string? Command = null,
        string? Path = null,
        string? Content = null,
        string? Pattern = null,
        string? Query = null,
        string? Url = null,
        string? ArgumentKey = null);

    [TestMethod]
    public void ExtensionData_SurvivesRoundTrip()
    {
        // Simulate what happens when CopilotHookAdapter deserializes toolArgs with unknown keys
        var toolInputJson = """{"request":{"plan_id":"fix-typos"},"description":"test"}""";
        var toolInput = JsonSerializer.Deserialize(toolInputJson, BouncerJsonContext.Default.ToolInput);

        toolInput.Should().NotBeNull();
        toolInput!.ExtensionData.Should().ContainKey("request");
        toolInput.ExtensionData.Should().ContainKey("description");

        // Now serialize it back — this is what BuildPrompt and MaybeLog do
        var reserialized = JsonSerializer.Serialize(toolInput, BouncerJsonContext.Default.ToolInput);

        reserialized.Should().Contain("request");
        reserialized.Should().Contain("fix-typos");
        reserialized.Should().Contain("description");
    }

    [TestMethod]
    public void ExtensionData_MixedKnownAndUnknown_BothSerialize()
    {
        // Tool like sql that has both a known property (query) and unknown ones (description)
        var toolInputJson = """{"query":"UPDATE todos SET status = 'done'","description":"mark done"}""";
        var toolInput = JsonSerializer.Deserialize(toolInputJson, BouncerJsonContext.Default.ToolInput);

        toolInput.Should().NotBeNull();
        toolInput!.Query.Should().Be("UPDATE todos SET status = 'done'");
        toolInput.ExtensionData.Should().ContainKey("description");

        var reserialized = JsonSerializer.Serialize(toolInput, BouncerJsonContext.Default.ToolInput);

        reserialized.Should().Contain("query");
        reserialized.Should().Contain("UPDATE todos");
        reserialized.Should().Contain("description");
        reserialized.Should().Contain("mark done");
    }

    [TestMethod]
    public void ExtensionData_NoUnknownKeys_SerializesCleanly()
    {
        var toolInputJson = """{"command":"echo hi"}""";
        var toolInput = JsonSerializer.Deserialize(toolInputJson, BouncerJsonContext.Default.ToolInput);

        toolInput.Should().NotBeNull();
        toolInput!.Command.Should().Be("echo hi");
        toolInput.ExtensionData.Should().BeNull();

        var reserialized = JsonSerializer.Serialize(toolInput, BouncerJsonContext.Default.ToolInput);

        reserialized.Should().Be("""{"command":"echo hi"}""");
    }
}
