using System.Text.Json;
using Bouncer.Models;

namespace Bouncer.Pipeline;

/// <summary>
/// Adapter for Copilot CLI hook format.
/// Input:  { "toolName": "bash", "toolArgs": "{\"command\":\"...\"}", "cwd": "..." }
/// Output: { "permissionDecision": "...", "permissionDecisionReason": "..." }
/// Exit:   0 = allow, 2 = deny
/// </summary>
public sealed class CopilotHookAdapter : IHookAdapter
{
    public bool CanHandle(JsonElement root) => root.TryGetProperty("toolName", out _);

    public HookInput? ReadInput(JsonElement root)
    {
        if (!root.TryGetProperty("toolName", out var toolNameProp))
            return null;

        var toolName = toolNameProp.GetString();
        if (toolName is null)
            return null;

        ToolInput? toolInput = null;
        if (root.TryGetProperty("toolArgs", out var toolArgsProp))
        {
            if (toolArgsProp.ValueKind == JsonValueKind.String)
            {
                var toolArgsStr = toolArgsProp.GetString();
                if (toolArgsStr is not null)
                {
                    toolInput = JsonSerializer.Deserialize(toolArgsStr, BouncerJsonContext.Default.ToolInput);
                }
            }
            else if (toolArgsProp.ValueKind == JsonValueKind.Object)
            {
                toolInput = toolArgsProp.Deserialize(BouncerJsonContext.Default.ToolInput);
            }
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
            ? HookSpecificOutput.Deny(result.Reason)
            : HookSpecificOutput.Allow(result.Reason);

        await JsonSerializer.SerializeAsync(output, hookOutput, BouncerJsonContext.Default.HookSpecificOutput, cancellationToken);
        await output.FlushAsync(cancellationToken);
    }

    public int GetExitCode(EvaluationResult result) =>
        result.Decision == PermissionDecision.Deny ? 2 : 0;
}
