using System.Diagnostics;
using System.Text.Json;
using Bouncer.Models;
using Bouncer.Options;
using Bouncer.Pipeline;
using Bouncer.Rules;

namespace Bouncer.Commands;

public static class TestCommand
{
    public static async Task<int> ExecuteAsync(
        string[] args,
        IBouncerPipeline pipeline,
        IRuleEngine ruleEngine,
        BouncerOptions options,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>(args);
        var verbose = false;

        if (arguments.Count > 0 && (arguments[0] == "-v" || arguments[0] == "--verbose"))
        {
            verbose = true;
            arguments.RemoveAt(0);
        }

        if (arguments.Count < 2)
        {
            error.WriteLine("Usage: bouncer test [-v] <tool> <input>");
            return 1;
        }

        var toolName = arguments[0];
        var rawInput = string.Join(' ', arguments.Skip(1));

        if (string.IsNullOrWhiteSpace(rawInput))
        {
            error.WriteLine("Input is required.");
            return 1;
        }

        if (!TryCreateHookInput(toolName, rawInput, error, out var hookInput) || hookInput is null)
        {
            return 1;
        }

        var stopwatch = Stopwatch.StartNew();
        var result = await pipeline.EvaluateAsync(hookInput, cancellationToken);
        stopwatch.Stop();

        var ruleMatch = ruleEngine.Evaluate(hookInput);
        var decisionText = result.Decision == PermissionDecision.Deny ? "DENY" : "ALLOW";
        var tierLabel = FormatTierLabel(result.Tier);

        if (!verbose)
        {
            output.WriteLine($"{decisionText} ({tierLabel}) - {result.Reason}");
            return result.Decision == PermissionDecision.Deny ? 2 : 0;
        }

        output.WriteLine($"{decisionText} ({tierLabel})");
        output.WriteLine($"  Tool:    {hookInput.ToolName}");
        output.WriteLine($"  Input:   {rawInput}");

        if (ruleMatch is not null)
        {
            output.WriteLine($"  Rule:    {ruleMatch.Rule.Name}");
            output.WriteLine($"  Pattern: {ruleMatch.Rule.Pattern}");
        }

        output.WriteLine($"  Tier:    {FormatTierDetail(result.Tier)}");
        output.WriteLine($"  Time:    {stopwatch.ElapsedMilliseconds}ms");

        return result.Decision == PermissionDecision.Deny ? 2 : 0;
    }

    private static string FormatTierLabel(EvaluationTier tier) =>
        tier switch
        {
            EvaluationTier.Rules => "Tier 1, regex",
            EvaluationTier.Llm => "Tier 2, LLM",
            _ => "default"
        };

    private static string FormatTierDetail(EvaluationTier tier) =>
        tier switch
        {
            EvaluationTier.Rules => "1 (regex)",
            EvaluationTier.Llm => "2 (LLM)",
            _ => "default"
        };

    private static bool TryCreateHookInput(
        string toolName,
        string rawInput,
        TextWriter error,
        out HookInput? hookInput)
    {
        var normalizedTool = toolName.Trim();
        var toolKey = normalizedTool.ToLowerInvariant();

        hookInput = toolKey switch
        {
            "bash" => HookInput.Bash(rawInput),
            "read" => HookInput.Read(rawInput),
            "write" => HookInput.Write(ParsePath(rawInput), ParseContent(rawInput)),
            "edit" => HookInput.Edit(ParsePath(rawInput), ParseContent(rawInput)),
            "glob" => CreateWithPattern(toolKey, rawInput, error, (path, pattern) => HookInput.Glob(path, pattern)),
            "grep" => CreateWithPattern(toolKey, rawInput, error, (path, pattern) => HookInput.Grep(path, pattern)),
            "webfetch" => HookInput.WebFetch(rawInput),
            "websearch" => HookInput.WebSearch(rawInput),
            _ when toolKey.StartsWith("mcp", StringComparison.OrdinalIgnoreCase) =>
                TryCreateMcpInput(normalizedTool, rawInput, error),
            _ => default
        };

        if (hookInput is null)
        {
            if (toolKey is "glob" or "grep" || toolKey.StartsWith("mcp", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            error.WriteLine($"Unsupported tool type: {toolName}");
            return false;
        }

        return true;
    }

    private static HookInput CreateWithPattern(
        string toolName,
        string rawInput,
        TextWriter error,
        Func<string, string, HookInput> factory)
    {
        var (path, pattern) = SplitTwoParts(rawInput);
        if (string.IsNullOrWhiteSpace(pattern))
        {
            error.WriteLine($"Usage for {toolName}: <path> <pattern>");
            return default!;
        }

        return factory(path, pattern);
    }

    private static string ParsePath(string rawInput) =>
        SplitTwoParts(rawInput).path;

    private static string ParseContent(string rawInput)
    {
        var (_, content) = SplitTwoParts(rawInput);
        return string.IsNullOrWhiteSpace(content) ? string.Empty : content;
    }

    private static (string path, string pattern) SplitTwoParts(string rawInput)
    {
        var index = rawInput.IndexOf(' ');
        if (index < 0)
        {
            return (rawInput, string.Empty);
        }

        var first = rawInput[..index];
        var second = rawInput[(index + 1)..];
        return (first, second);
    }

    private static HookInput? TryCreateMcpInput(string toolName, string rawInput, TextWriter error)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return HookInput.Mcp(toolName, new Dictionary<string, JsonElement>());
        }

        try
        {
            using var document = JsonDocument.Parse(rawInput);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error.WriteLine("MCP arguments must be a JSON object.");
                return null;
            }

            var arguments = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                arguments[property.Name] = property.Value.Clone();
            }

            return HookInput.Mcp(toolName, arguments);
        }
        catch (JsonException)
        {
            error.WriteLine("Invalid MCP arguments JSON.");
            return null;
        }
    }
}
