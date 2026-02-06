using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bouncer.Models;

public sealed class ToolInput
{
    [JsonPropertyName("command")]
    public string? Command { get; init; }

    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; init; }

    [JsonPropertyName("query")]
    public string? Query { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("arguments")]
    public Dictionary<string, JsonElement>? Arguments { get; init; }

    public static ToolInput ForCommand(string command) => new()
    {
        Command = command
    };

    public static ToolInput ForPath(string path) => new()
    {
        Path = path
    };

    public static ToolInput ForPathAndContent(string path, string content) => new()
    {
        Path = path,
        Content = content
    };

    public static ToolInput ForPathAndPattern(string path, string pattern) => new()
    {
        Path = path,
        Pattern = pattern
    };

    public static ToolInput ForQuery(string query) => new()
    {
        Query = query
    };

    public static ToolInput ForUrl(string url) => new()
    {
        Url = url
    };

    public static ToolInput ForArguments(Dictionary<string, JsonElement> arguments) => new()
    {
        Arguments = arguments
    };
}

public sealed class HookInput
{
    [JsonPropertyName("tool_name")]
    public string ToolName { get; init; } = string.Empty;

    [JsonPropertyName("tool_input")]
    public ToolInput ToolInput { get; init; } = new();

    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }

    public static HookInput Bash(string command, string? cwd = null) => new()
    {
        ToolName = "bash",
        ToolInput = ToolInput.ForCommand(command),
        Cwd = cwd
    };

    public static HookInput Write(string path, string content, string? cwd = null) => new()
    {
        ToolName = "write",
        ToolInput = ToolInput.ForPathAndContent(path, content),
        Cwd = cwd
    };

    public static HookInput Edit(string path, string content, string? cwd = null) => new()
    {
        ToolName = "edit",
        ToolInput = ToolInput.ForPathAndContent(path, content),
        Cwd = cwd
    };

    public static HookInput Read(string path, string? cwd = null) => new()
    {
        ToolName = "read",
        ToolInput = ToolInput.ForPath(path),
        Cwd = cwd
    };

    public static HookInput Glob(string path, string pattern, string? cwd = null) => new()
    {
        ToolName = "glob",
        ToolInput = ToolInput.ForPathAndPattern(path, pattern),
        Cwd = cwd
    };

    public static HookInput Grep(string path, string pattern, string? cwd = null) => new()
    {
        ToolName = "grep",
        ToolInput = ToolInput.ForPathAndPattern(path, pattern),
        Cwd = cwd
    };

    public static HookInput WebFetch(string url, string? cwd = null) => new()
    {
        ToolName = "webfetch",
        ToolInput = ToolInput.ForUrl(url),
        Cwd = cwd
    };

    public static HookInput WebSearch(string query, string? cwd = null) => new()
    {
        ToolName = "websearch",
        ToolInput = ToolInput.ForQuery(query),
        Cwd = cwd
    };

    public static HookInput Mcp(string toolName, Dictionary<string, JsonElement> arguments, string? cwd = null) => new()
    {
        ToolName = toolName,
        ToolInput = ToolInput.ForArguments(arguments),
        Cwd = cwd
    };
}
