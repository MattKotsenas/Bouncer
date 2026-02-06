using Bouncer.Options;
using Bouncer.Rules;

namespace Bouncer.Commands;

public static class CheckCommand
{
    public static int Execute(BouncerOptions options, TextWriter output)
    {
        var enabledGroups = DefaultRuleGroups.All
            .Where(group =>
                options.RuleGroups.TryGetValue(group.Name, out var groupOptions)
                    ? groupOptions.Enabled
                    : true)
            .ToList();

        var disabledGroups = DefaultRuleGroups.All.Count - enabledGroups.Count;
        var defaultRuleCount = enabledGroups.Sum(group => group.Rules.Count);
        var customRuleCount = options.CustomRules?.Count ?? 0;

        output.WriteLine($"DefaultAction: {options.DefaultAction}");
        output.WriteLine($"Rule groups: {enabledGroups.Count} enabled, {disabledGroups} disabled");
        output.WriteLine($"Default rules: {defaultRuleCount}");
        output.WriteLine($"Custom rules: {customRuleCount}");
        output.WriteLine($"Total rules: {defaultRuleCount + customRuleCount}");
        output.WriteLine($"LLM fallback: {(options.LlmFallback.Enabled ? "enabled" : "disabled")}");
        output.WriteLine("Provider chain:");

        if (options.LlmFallback.ProviderChain.Count == 0)
        {
            output.WriteLine("  (none)");
        }
        else
        {
            var index = 1;
            foreach (var provider in options.LlmFallback.ProviderChain)
            {
                var model = string.IsNullOrWhiteSpace(provider.Model) ? "n/a" : provider.Model;
                output.WriteLine(
                    $"  {index}. {provider.Type} (model={model}, timeout={provider.TimeoutSeconds}s)");
                index++;
            }
        }

        return 0;
    }
}
