using System.Text.Json.Serialization;

namespace Bouncer.Options;

public sealed class BouncerOptions
{
    public const string SectionName = "";

    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    [JsonPropertyName("defaultAction")]
    public string DefaultAction { get; set; } = "allow";

    [JsonPropertyName("ruleGroups")]
    public Dictionary<string, RuleGroupOptions> RuleGroups { get; set; } = DefaultRuleGroups();

    [JsonPropertyName("customRules")]
    public List<CustomRuleOptions> CustomRules { get; set; } = [];

    [JsonPropertyName("llmFallback")]
    public LlmFallbackOptions LlmFallback { get; set; } = new();

    private static Dictionary<string, RuleGroupOptions> DefaultRuleGroups() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["bash"] = new RuleGroupOptions(),
            ["powershell"] = new RuleGroupOptions(),
            ["builtins"] = new RuleGroupOptions(),
            ["git"] = new RuleGroupOptions(),
            ["secrets-exposure"] = new RuleGroupOptions(),
            ["production-risk"] = new RuleGroupOptions(),
            ["web"] = new RuleGroupOptions()
        };
}

public sealed class RuleGroupOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}

public sealed class CustomRuleOptions
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("toolMatch")]
    public string ToolMatch { get; set; } = string.Empty;

    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = "deny";

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public sealed class LlmFallbackOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("maxTokens")]
    public int MaxTokens { get; set; } = 150;

    [JsonPropertyName("confidenceThreshold")]
    public double ConfidenceThreshold { get; set; } = 0.7;

    [JsonPropertyName("providerChain")]
    public List<LlmProviderOptions> ProviderChain { get; set; } = DefaultProviderChain();

    private static List<LlmProviderOptions> DefaultProviderChain() =>
        [
            new LlmProviderOptions
            {
                Type = "anthropic",
                Model = "claude-haiku-4-5-20251001",
                TimeoutSeconds = 2
            },
            new LlmProviderOptions
            {
                Type = "github-models",
                Model = "gpt-4o-mini",
                TimeoutSeconds = 2
            },
            new LlmProviderOptions
            {
                Type = "openai",
                Model = "gpt-4o-mini",
                TimeoutSeconds = 2
            },
            new LlmProviderOptions
            {
                Type = "ollama",
                Endpoint = "http://localhost:11434",
                Model = "llama3:8b",
                TimeoutSeconds = 3
            }
        ];
}

public sealed class LlmProviderOptions
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 2;

    [JsonPropertyName("apiKeyCommand")]
    public string? ApiKeyCommand { get; set; }
}
