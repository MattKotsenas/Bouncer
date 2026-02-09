using System.Diagnostics;
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

        var toolName = arguments[0].Trim();
        var rawInput = string.Join(' ', arguments.Skip(1));

        if (string.IsNullOrWhiteSpace(rawInput))
        {
            error.WriteLine("Input is required.");
            return 1;
        }

        var hookInput = CreateHookInput(toolName, rawInput);

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

    internal static HookInput CreateHookInput(string toolName, string rawInput)
    {
        var field = RegexRuleEngine.ResolveField(toolName);
        var toolInput = field switch
        {
            ToolField.Path => ToolInput.ForPathAndContent(SplitFirst(rawInput).first, SplitFirst(rawInput).rest),
            ToolField.Pattern => ToolInput.ForPathAndPattern(SplitFirst(rawInput).first, SplitFirst(rawInput).rest),
            ToolField.Url => ToolInput.ForUrl(rawInput),
            ToolField.Query => ToolInput.ForQuery(rawInput),
            _ => ToolInput.ForCommand(rawInput)
        };

        return new HookInput { ToolName = toolName, ToolInput = toolInput };
    }

    private static (string first, string rest) SplitFirst(string input)
    {
        var index = input.IndexOf(' ');
        return index < 0
            ? (input, string.Empty)
            : (input[..index], input[(index + 1)..]);
    }
}
