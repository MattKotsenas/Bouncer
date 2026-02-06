using System.Text.Json.Serialization;

namespace Bouncer.Models;

public sealed class HookOutput
{
    [JsonPropertyName("hookSpecificOutput")]
    public HookSpecificOutput HookSpecificOutput { get; init; } = new();

    public static HookOutput Allow(string reason) => new()
    {
        HookSpecificOutput = HookSpecificOutput.Allow(reason)
    };

    public static HookOutput Deny(string reason) => new()
    {
        HookSpecificOutput = HookSpecificOutput.Deny(reason)
    };
}

public sealed class HookSpecificOutput
{
    [JsonPropertyName("hookEventName")]
    public string HookEventName { get; init; } = "PreToolUse";

    [JsonPropertyName("permissionDecision")]
    public string PermissionDecision { get; init; } = "allow";

    [JsonPropertyName("permissionDecisionReason")]
    public string PermissionDecisionReason { get; init; } = string.Empty;

    public static HookSpecificOutput Allow(string reason) => new()
    {
        PermissionDecision = "allow",
        PermissionDecisionReason = reason
    };

    public static HookSpecificOutput Deny(string reason) => new()
    {
        PermissionDecision = "deny",
        PermissionDecisionReason = reason
    };
}
