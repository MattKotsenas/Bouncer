using System.Text;
using System.Text.Json;
using Bouncer.Models;
using Bouncer.Pipeline;
using FluentAssertions;

namespace Bouncer.Tests;

[TestClass]
public sealed class HookAdapterTests
{
    // ── Factory ───────────────────────────────────────────────

    [TestMethod]
    public void Factory_ClaudeFormat_ReturnsClaudeAdapter()
    {
        var json = """{"tool_name":"Bash","tool_input":{"command":"echo hi"},"cwd":"/tmp"}""";
        using var doc = JsonDocument.Parse(json);
        var factory = CreateFactory();

        factory.Create(doc.RootElement).Should().BeOfType<ClaudeHookAdapter>();
    }

    [TestMethod]
    public void Factory_CopilotFormat_ReturnsCopilotAdapter()
    {
        var json = """{"toolName":"bash","toolArgs":"{\"command\":\"echo hi\"}","cwd":"/tmp"}""";
        using var doc = JsonDocument.Parse(json);
        var factory = CreateFactory();

        factory.Create(doc.RootElement).Should().BeOfType<CopilotHookAdapter>();
    }

    [TestMethod]
    public void Factory_EmptyObject_DefaultsToFirst()
    {
        using var doc = JsonDocument.Parse("{}");
        var factory = CreateFactory();

        factory.Create(doc.RootElement).Should().BeOfType<ClaudeHookAdapter>();
    }

    [TestMethod]
    public void Factory_Default_ReturnsFirstAdapter()
    {
        var factory = CreateFactory();

        factory.Default.Should().BeOfType<ClaudeHookAdapter>();
    }

    // ── CanHandle ─────────────────────────────────────────────

    [TestMethod]
    public void Claude_CanHandle_TrueForSnakeCase()
    {
        using var doc = JsonDocument.Parse("""{"tool_name":"Bash"}""");

        new ClaudeHookAdapter().CanHandle(doc.RootElement).Should().BeTrue();
    }

    [TestMethod]
    public void Claude_CanHandle_FalseForCamelCase()
    {
        using var doc = JsonDocument.Parse("""{"toolName":"bash"}""");

        new ClaudeHookAdapter().CanHandle(doc.RootElement).Should().BeFalse();
    }

    [TestMethod]
    public void Copilot_CanHandle_TrueForCamelCase()
    {
        using var doc = JsonDocument.Parse("""{"toolName":"bash"}""");

        new CopilotHookAdapter().CanHandle(doc.RootElement).Should().BeTrue();
    }

    [TestMethod]
    public void Copilot_CanHandle_FalseForSnakeCase()
    {
        using var doc = JsonDocument.Parse("""{"tool_name":"Bash"}""");

        new CopilotHookAdapter().CanHandle(doc.RootElement).Should().BeFalse();
    }

    // ── Claude adapter: ReadInput ─────────────────────────────

    [TestMethod]
    public void Claude_ReadInput_ParsesSnakeCaseFields()
    {
        var json = """{"tool_name":"Bash","tool_input":{"command":"echo hi"},"cwd":"/tmp"}""";
        using var doc = JsonDocument.Parse(json);
        var adapter = new ClaudeHookAdapter();

        var input = adapter.ReadInput(doc.RootElement);

        input.Should().NotBeNull();
        input!.ToolName.Should().Be("Bash");
        input.ToolInput.Command.Should().Be("echo hi");
        input.Cwd.Should().Be("/tmp");
    }

    [TestMethod]
    public void Claude_ReadInput_PreservesUnknownToolInput()
    {
        var json = """{"tool_name":"ledger_append","tool_input":{"request":{"plan_id":"fix-typos"}},"cwd":"/tmp"}""";
        using var doc = JsonDocument.Parse(json);
        var adapter = new ClaudeHookAdapter();

        var input = adapter.ReadInput(doc.RootElement);

        input.Should().NotBeNull();
        input!.ToolInput.ExtensionData.Should().ContainKey("request");
        input.ToolInput.ExtensionData!["request"].GetProperty("plan_id").GetString().Should().Be("fix-typos");
    }

    [TestMethod]
    public void Claude_ReadInput_MissingToolName_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("""{"tool_input":{"command":"echo"}}""");

        new ClaudeHookAdapter().ReadInput(doc.RootElement).Should().BeNull();
    }

    // ── Claude adapter: WriteOutput ───────────────────────────

    [TestMethod]
    public async Task Claude_WriteOutput_WrapsInHookSpecificOutput()
    {
        var adapter = new ClaudeHookAdapter();
        var result = new EvaluationResult(PermissionDecision.Deny, EvaluationTier.Rules, "dangerous");

        using var stream = new MemoryStream();
        await adapter.WriteOutputAsync(stream, result, CancellationToken.None);

        var output = Encoding.UTF8.GetString(stream.ToArray());
        output.Should().Contain("\"hookSpecificOutput\"");
        output.Should().Contain("\"permissionDecision\":\"deny\"");
        output.Should().Contain("\"permissionDecisionReason\":\"dangerous\"");
    }

    [TestMethod]
    public void Claude_ExitCode_Allow0_Deny2()
    {
        var adapter = new ClaudeHookAdapter();

        adapter.GetExitCode(new EvaluationResult(PermissionDecision.Allow, EvaluationTier.Rules, "ok")).Should().Be(0);
        adapter.GetExitCode(new EvaluationResult(PermissionDecision.Deny, EvaluationTier.Rules, "bad")).Should().Be(2);
    }

    // ── Copilot adapter: ReadInput ────────────────────────────

    [TestMethod]
    public void Copilot_ReadInput_ParsesCamelCaseWithStringToolArgs()
    {
        var json = """{"toolName":"bash","toolArgs":"{\"command\":\"echo hi\"}","cwd":"/tmp"}""";
        using var doc = JsonDocument.Parse(json);
        var adapter = new CopilotHookAdapter();

        var input = adapter.ReadInput(doc.RootElement);

        input.Should().NotBeNull();
        input!.ToolName.Should().Be("bash");
        input.ToolInput.Command.Should().Be("echo hi");
        input.Cwd.Should().Be("/tmp");
    }

    [TestMethod]
    public void Copilot_ReadInput_ParsesObjectToolArgs()
    {
        var json = """{"toolName":"bash","toolArgs":{"command":"echo hi"},"cwd":"/tmp"}""";
        using var doc = JsonDocument.Parse(json);
        var adapter = new CopilotHookAdapter();

        var input = adapter.ReadInput(doc.RootElement);

        input.Should().NotBeNull();
        input!.ToolName.Should().Be("bash");
        input.ToolInput.Command.Should().Be("echo hi");
    }

    [TestMethod]
    public void Copilot_ReadInput_PreservesUnknownToolArgs_StringFormat()
    {
        var json = """{"toolName":"ledger_append","toolArgs":"{\"request\":{\"plan_id\":\"fix-typos\"}}","cwd":"/tmp"}""";
        using var doc = JsonDocument.Parse(json);
        var adapter = new CopilotHookAdapter();

        var input = adapter.ReadInput(doc.RootElement);

        input.Should().NotBeNull();
        input!.ToolName.Should().Be("ledger_append");
        input.ToolInput.ExtensionData.Should().ContainKey("request");
        input.ToolInput.ExtensionData!["request"].GetProperty("plan_id").GetString().Should().Be("fix-typos");
    }

    [TestMethod]
    public void Copilot_ReadInput_PreservesUnknownToolArgs_ObjectFormat()
    {
        var json = """{"toolName":"task","toolArgs":{"prompt":"find files","agent_type":"explore"},"cwd":"/tmp"}""";
        using var doc = JsonDocument.Parse(json);
        var adapter = new CopilotHookAdapter();

        var input = adapter.ReadInput(doc.RootElement);

        input.Should().NotBeNull();
        input!.ToolInput.ExtensionData.Should().ContainKey("prompt");
        input.ToolInput.ExtensionData.Should().ContainKey("agent_type");
        input.ToolInput.ExtensionData!["prompt"].GetString().Should().Be("find files");
    }

    [TestMethod]
    public void Copilot_ReadInput_MixedKnownAndUnknownArgs_PreservesBoth()
    {
        var json = """{"toolName":"powershell","toolArgs":"{\"command\":\"dotnet build\",\"description\":\"Build project\",\"mode\":\"sync\"}","cwd":"/tmp"}""";
        using var doc = JsonDocument.Parse(json);
        var adapter = new CopilotHookAdapter();

        var input = adapter.ReadInput(doc.RootElement);

        input.Should().NotBeNull();
        input!.ToolInput.Command.Should().Be("dotnet build");
        input.ToolInput.ExtensionData.Should().ContainKey("description");
        input.ToolInput.ExtensionData.Should().ContainKey("mode");
        input.ToolInput.ExtensionData.Should().NotContainKey("command", "known properties should not appear in ExtensionData");
    }

    [TestMethod]
    public void Copilot_ReadInput_StandardToolArgs_NoExtensionData()
    {
        var json = """{"toolName":"bash","toolArgs":"{\"command\":\"echo hi\"}","cwd":"/tmp"}""";
        using var doc = JsonDocument.Parse(json);
        var adapter = new CopilotHookAdapter();

        var input = adapter.ReadInput(doc.RootElement);

        input.Should().NotBeNull();
        input!.ToolInput.Command.Should().Be("echo hi");
        input.ToolInput.ExtensionData.Should().BeNull();
    }

    [TestMethod]
    public void Copilot_ReadInput_MissingToolName_ReturnsNull()
    {
        var json = """{"toolArgs":"{\"command\":\"echo\"}"}""";
        using var doc = JsonDocument.Parse(json);
        var adapter = new CopilotHookAdapter();

        adapter.ReadInput(doc.RootElement).Should().BeNull();
    }

    [TestMethod]
    public void Copilot_ReadInput_MissingToolArgs_ReturnsEmptyToolInput()
    {
        var json = """{"toolName":"bash"}""";
        using var doc = JsonDocument.Parse(json);
        var adapter = new CopilotHookAdapter();

        var input = adapter.ReadInput(doc.RootElement);

        input.Should().NotBeNull();
        input!.ToolName.Should().Be("bash");
        input.ToolInput.Command.Should().BeNull();
    }

    // ── Copilot adapter: apply_patch parsing ─────────────────

    [TestMethod]
    public void Copilot_ReadInput_ParsesApplyPatchUpdateFile()
    {
        var patchArgs = "*** Begin Patch\n*** Update File: C:\\Projects\\src\\Program.cs\n@@\n- old\n+ new\n";
        var json = $$"""{"toolName":"apply_patch","toolArgs":{{JsonSerializer.Serialize(patchArgs)}},"cwd":"C:\\Projects"}""";
        using var doc = JsonDocument.Parse(json);
        var adapter = new CopilotHookAdapter();

        var input = adapter.ReadInput(doc.RootElement);

        input.Should().NotBeNull();
        input!.ToolName.Should().Be("apply_patch");
        input.ToolInput.Path.Should().Be(@"C:\Projects\src\Program.cs");
    }

    [TestMethod]
    public void Copilot_ReadInput_ParsesApplyPatchAddFile()
    {
        var patchArgs = "*** Begin Patch\n*** Add File: C:\\Projects\\src\\NewFile.cs\n+ content\n";
        var json = $$"""{"toolName":"apply_patch","toolArgs":{{JsonSerializer.Serialize(patchArgs)}},"cwd":"C:\\Projects"}""";
        using var doc = JsonDocument.Parse(json);
        var adapter = new CopilotHookAdapter();

        var input = adapter.ReadInput(doc.RootElement);

        input.Should().NotBeNull();
        input!.ToolInput.Path.Should().Be(@"C:\Projects\src\NewFile.cs");
    }

    [TestMethod]
    public void Copilot_ReadInput_ParsesApplyPatchDeleteFile()
    {
        var patchArgs = "*** Begin Patch\n*** Delete File: C:\\Projects\\old.txt\n";
        var json = $$"""{"toolName":"apply_patch","toolArgs":{{JsonSerializer.Serialize(patchArgs)}},"cwd":"C:\\Projects"}""";
        using var doc = JsonDocument.Parse(json);
        var adapter = new CopilotHookAdapter();

        var input = adapter.ReadInput(doc.RootElement);

        input.Should().NotBeNull();
        input!.ToolInput.Path.Should().Be(@"C:\Projects\old.txt");
    }

    [TestMethod]
    public void Copilot_ReadInput_ApplyPatchWithNoFileHeader_ReturnsEmptyToolInput()
    {
        var patchArgs = "some random non-json non-patch text";
        var json = $$"""{"toolName":"apply_patch","toolArgs":{{JsonSerializer.Serialize(patchArgs)}}}""";
        using var doc = JsonDocument.Parse(json);
        var adapter = new CopilotHookAdapter();

        var input = adapter.ReadInput(doc.RootElement);

        input.Should().NotBeNull();
        input!.ToolInput.Path.Should().BeNull();
    }

    // ── Copilot adapter: WriteOutput ──────────────────────────

    [TestMethod]
    public async Task Copilot_WriteOutput_WritesBarePermissionDecision()
    {
        var adapter = new CopilotHookAdapter();
        var result = new EvaluationResult(PermissionDecision.Deny, EvaluationTier.Rules, "blocked");

        using var stream = new MemoryStream();
        await adapter.WriteOutputAsync(stream, result, CancellationToken.None);

        var output = Encoding.UTF8.GetString(stream.ToArray());
        output.Should().NotContain("hookSpecificOutput");
        output.Should().Contain("\"permissionDecision\":\"deny\"");
        output.Should().Contain("\"permissionDecisionReason\":\"blocked\"");
    }

    [TestMethod]
    public void Copilot_ExitCode_Allow0_Deny2()
    {
        var adapter = new CopilotHookAdapter();

        adapter.GetExitCode(new EvaluationResult(PermissionDecision.Allow, EvaluationTier.Rules, "ok")).Should().Be(0);
        adapter.GetExitCode(new EvaluationResult(PermissionDecision.Deny, EvaluationTier.Rules, "bad")).Should().Be(2);
    }

    // ── Helpers ───────────────────────────────────────────────

    private static HookAdapterFactory CreateFactory() =>
        new([new ClaudeHookAdapter(), new CopilotHookAdapter()]);
}
