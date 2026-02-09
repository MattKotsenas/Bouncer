using System.Text.RegularExpressions;
using Bouncer.Models;
using Bouncer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bouncer.Rules;

public sealed class RegexRuleEngine : IRuleEngine
{
    private readonly IReadOnlyList<CompiledRule> _rules;
    private readonly ILogger<RegexRuleEngine> _logger;

    public RegexRuleEngine(IOptions<BouncerOptions> options, ILogger<RegexRuleEngine> logger)
    {
        _logger = logger;
        _rules = BuildRules(options.Value);
    }

    public RuleMatch? Evaluate(HookInput input)
    {
        var inputToolName = CanonicalizeToolName(input.ToolName);

        foreach (var rule in _rules)
        {
            if (!string.Equals(rule.CanonicalToolName, inputToolName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = GetFieldValue(input, rule.Rule.Field);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!rule.Regex.IsMatch(value))
            {
                continue;
            }

            return new RuleMatch(rule.GroupName, rule.Rule, rule.Decision, rule.Reason);
        }

        return null;
    }

    private IReadOnlyList<CompiledRule> BuildRules(BouncerOptions options)
    {
        var rules = new List<CompiledRule>();

        foreach (var group in DefaultRuleGroups.All)
        {
            var groupOptions = options.RuleGroups.TryGetValue(group.Name, out var configured)
                ? configured
                : new RuleGroupOptions();

            if (!groupOptions.Enabled)
            {
                continue;
            }

            foreach (var rule in group.Rules)
            {
                rules.Add(new CompiledRule(rule, group.Name, ParseAction(rule.Action), rule.Reason, CanonicalizeToolName(rule.ToolName)));
            }
        }

        if (options.CustomRules is not null)
        {
            foreach (var custom in options.CustomRules)
            {
                var reason = string.IsNullOrWhiteSpace(custom.Reason)
                    ? $"Matched custom rule '{custom.Name}'"
                    : custom.Reason;

                var field = ResolveField(custom.ToolMatch);
                if (field == ToolField.Unknown)
                {
                    _logger.LogDebug(
                        "Skipping custom rule {RuleName}; unknown tool match {ToolName}",
                        custom.Name,
                        custom.ToolMatch);
                    continue;
                }

                var rule = new RuleDefinition(
                    custom.Name,
                    custom.ToolMatch,
                    field,
                    custom.Pattern,
                    custom.Action,
                    reason);

                rules.Add(new CompiledRule(rule, "custom", ParseAction(custom.Action), reason, CanonicalizeToolName(rule.ToolName)));
            }
        }

        return rules;
    }

    private static PermissionDecision ParseAction(string? action) =>
        string.Equals(action, "deny", StringComparison.OrdinalIgnoreCase)
            ? PermissionDecision.Deny
            : PermissionDecision.Allow;

    internal static ToolField ResolveField(string toolName)
    {
        return CanonicalizeToolName(toolName).ToLowerInvariant() switch
        {
            "bash" => ToolField.Command,
            "pwsh" => ToolField.Command,
            "edit" => ToolField.Path,
            "write" => ToolField.Path,
            "read" => ToolField.Path,
            "glob" => ToolField.Pattern,
            "grep" => ToolField.Pattern,
            "webfetch" => ToolField.Url,
            "websearch" => ToolField.Query,
            _ => ToolField.Unknown
        };
    }

    private static string CanonicalizeToolName(string toolName) =>
        string.Equals(toolName, "powershell", StringComparison.OrdinalIgnoreCase)
            ? "pwsh"
            : toolName;

    private static string? GetFieldValue(HookInput input, ToolField field)
    {
        return field switch
        {
            ToolField.Command => input.ToolInput.Command,
            ToolField.Path => input.ToolInput.Path,
            ToolField.Content => input.ToolInput.Content,
            ToolField.Pattern => input.ToolInput.Pattern,
            ToolField.Query => input.ToolInput.Query,
            ToolField.Url => input.ToolInput.Url,
            _ => null
        };
    }

    private sealed record CompiledRule(
        RuleDefinition Rule,
        string GroupName,
        PermissionDecision Decision,
        string Reason,
        string CanonicalToolName)
    {
        public Regex Regex { get; } =
            Rule.RegexFactory?.Invoke()
            ?? new Regex(Rule.Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
