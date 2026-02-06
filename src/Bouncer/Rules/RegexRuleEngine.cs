using System.Text.RegularExpressions;
using Bouncer.Models;
using Bouncer.Options;
using Microsoft.Extensions.Options;

namespace Bouncer.Rules;

public sealed class RegexRuleEngine : IRuleEngine
{
    private readonly IReadOnlyList<CompiledRule> _rules;

    public RegexRuleEngine(IOptions<BouncerOptions> options)
    {
        _rules = BuildRules(options.Value);
    }

    public RuleMatch? Evaluate(HookInput input)
    {
        foreach (var rule in _rules)
        {
            if (!string.Equals(rule.Rule.ToolName, input.ToolName, StringComparison.OrdinalIgnoreCase))
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

    private static IReadOnlyList<CompiledRule> BuildRules(BouncerOptions options)
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

            var decision = ParseAction(groupOptions.Action);
            foreach (var rule in group.Rules)
            {
                rules.Add(new CompiledRule(rule, group.Name, decision, rule.Reason));
            }
        }

        if (options.CustomRules is not null)
        {
            foreach (var custom in options.CustomRules)
            {
                var reason = string.IsNullOrWhiteSpace(custom.Reason)
                    ? $"Matched custom rule '{custom.Name}'"
                    : custom.Reason;

                var rule = new RuleDefinition(
                    custom.Name,
                    custom.ToolMatch,
                    ResolveField(custom.ToolMatch),
                    custom.Pattern,
                    reason);

                rules.Add(new CompiledRule(rule, "custom", ParseAction(custom.Action), reason));
            }
        }

        return rules;
    }

    private static PermissionDecision ParseAction(string? action) =>
        string.Equals(action, "deny", StringComparison.OrdinalIgnoreCase)
            ? PermissionDecision.Deny
            : PermissionDecision.Allow;

    private static ToolField ResolveField(string toolName)
    {
        return toolName.ToLowerInvariant() switch
        {
            "bash" => ToolField.Command,
            "edit" => ToolField.Path,
            "write" => ToolField.Path,
            "read" => ToolField.Path,
            "glob" => ToolField.Pattern,
            "grep" => ToolField.Pattern,
            "webfetch" => ToolField.Url,
            "websearch" => ToolField.Query,
            _ => ToolField.Command
        };
    }

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
        string Reason)
    {
        public Regex Regex { get; } =
            new(Rule.Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
