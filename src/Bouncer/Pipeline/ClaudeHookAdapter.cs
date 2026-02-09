using System.Text.Json;
using Bouncer.Models;

namespace Bouncer.Pipeline;

/// <summary>
/// Adapter for Claude Code hook format.
/// Input:  { "tool_name": "Bash", "tool_input": { "command": "..." }, "cwd": "..." }
/// Output: { "hookSpecificOutput": { "permissionDecision": "...", ... } }
/// Exit:   0 = allow, 2 = deny
/// </summary>
public sealed class ClaudeHookAdapter : IHookAdapter
{
    public bool CanHandle(JsonElement root) => root.TryGetProperty("tool_name", out _);

    public HookInput? ReadInput(JsonElement root)
    {
        if (!root.TryGetProperty("tool_name", out var toolNameProp))
            return null;

        var toolName = toolNameProp.GetString();
        if (toolName is null)
            return null;

        ToolInput? toolInput = null;
        if (root.TryGetProperty("tool_input", out var toolInputProp) && toolInputProp.ValueKind == JsonValueKind.Object)
        {
            toolInput = toolInputProp.Deserialize(BouncerJsonContext.Default.ToolInput);
        }

        string? cwd = null;
        if (root.TryGetProperty("cwd", out var cwdProp))
        {
            cwd = cwdProp.GetString();
        }

        return new HookInput
        {
            ToolName = toolName,
            ToolInput = toolInput ?? new ToolInput(),
            Cwd = cwd
        };
    }

    public async Task WriteOutputAsync(Stream output, EvaluationResult result, CancellationToken cancellationToken)
    {
        var hookOutput = result.Decision == PermissionDecision.Deny
            ? HookOutput.Deny(result.Reason)
            : HookOutput.Allow(result.Reason);

        await JsonSerializer.SerializeAsync(output, hookOutput, BouncerJsonContext.Default.HookOutput, cancellationToken);
        await output.FlushAsync(cancellationToken);
    }

    public int GetExitCode(EvaluationResult result) =>
        result.Decision == PermissionDecision.Deny ? 2 : 0;
}
