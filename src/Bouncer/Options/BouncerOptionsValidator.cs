using Microsoft.Extensions.Options;

namespace Bouncer.Options;

public sealed class BouncerOptionsValidator : IValidateOptions<BouncerOptions>
{
    private static readonly HashSet<string> AllowedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "allow",
        "deny"
    };

    private static readonly HashSet<string> AllowedLogLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "denials-only",
        "all"
    };

    public ValidateOptionsResult Validate(string? name, BouncerOptions options)
    {
        var failures = new List<string>();

        if (options.Version < 1)
        {
            failures.Add("version must be >= 1");
        }

        if (!IsActionValid(options.DefaultAction))
        {
            failures.Add("defaultAction must be 'allow' or 'deny'");
        }

        if (options.RuleGroups is null)
        {
            failures.Add("ruleGroups is required");
        }
        else
        {
            foreach (var (groupName, group) in options.RuleGroups)
            {
                if (!IsActionValid(group.Action))
                {
                    failures.Add($"ruleGroups.{groupName}.action must be 'allow' or 'deny'");
                }
            }
        }

        if (options.CustomRules is not null)
        {
            for (var i = 0; i < options.CustomRules.Count; i++)
            {
                var rule = options.CustomRules[i];
                var prefix = $"customRules[{i}]";

                if (string.IsNullOrWhiteSpace(rule.Name))
                {
                    failures.Add($"{prefix}.name is required");
                }

                if (string.IsNullOrWhiteSpace(rule.ToolMatch))
                {
                    failures.Add($"{prefix}.toolMatch is required");
                }

                if (string.IsNullOrWhiteSpace(rule.Pattern))
                {
                    failures.Add($"{prefix}.pattern is required");
                }

                if (!IsActionValid(rule.Action))
                {
                    failures.Add($"{prefix}.action must be 'allow' or 'deny'");
                }
            }
        }

        if (options.LlmFallback is null)
        {
            failures.Add("llmFallback is required");
        }
        else if (options.LlmFallback.Enabled)
        {
            if (options.LlmFallback.MaxTokens <= 0)
            {
                failures.Add("llmFallback.maxTokens must be > 0");
            }

            if (options.LlmFallback.ConfidenceThreshold is < 0 or > 1)
            {
                failures.Add("llmFallback.confidenceThreshold must be between 0 and 1");
            }

            if (options.LlmFallback.ProviderChain is null || options.LlmFallback.ProviderChain.Count == 0)
            {
                failures.Add("llmFallback.providerChain must not be empty when enabled");
            }
            else
            {
                for (var i = 0; i < options.LlmFallback.ProviderChain.Count; i++)
                {
                    var provider = options.LlmFallback.ProviderChain[i];
                    var prefix = $"llmFallback.providerChain[{i}]";

                    if (string.IsNullOrWhiteSpace(provider.Type))
                    {
                        failures.Add($"{prefix}.type is required");
                    }

                    if (provider.TimeoutSeconds <= 0)
                    {
                        failures.Add($"{prefix}.timeoutSeconds must be > 0");
                    }
                }
            }
        }

        if (options.Logging is null)
        {
            failures.Add("logging is required");
        }
        else
        {
            if (!AllowedLogLevels.Contains(options.Logging.Level))
            {
                failures.Add("logging.level must be 'denials-only' or 'all'");
            }

            if (options.Logging.Enabled && string.IsNullOrWhiteSpace(options.Logging.Path))
            {
                failures.Add("logging.path is required when logging is enabled");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsActionValid(string? action) =>
        !string.IsNullOrWhiteSpace(action) && AllowedActions.Contains(action);
}
