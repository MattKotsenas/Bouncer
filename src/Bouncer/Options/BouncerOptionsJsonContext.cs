using System.Text.Json.Serialization;

namespace Bouncer.Options;

[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(BouncerOptions))]
[JsonSerializable(typeof(RuleGroupOptions))]
[JsonSerializable(typeof(CustomRuleOptions))]
[JsonSerializable(typeof(LlmFallbackOptions))]
[JsonSerializable(typeof(LlmProviderOptions))]
[JsonSerializable(typeof(LoggingOptions))]
public partial class BouncerOptionsJsonContext : JsonSerializerContext
{
}
