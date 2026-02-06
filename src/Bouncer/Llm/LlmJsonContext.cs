using System.Text.Json.Serialization;

namespace Bouncer.Llm;

public sealed class LlmDecisionResponse
{
    [JsonPropertyName("decision")]
    public string Decision { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }
}

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(LlmDecisionResponse))]
public partial class LlmJsonContext : JsonSerializerContext
{
}
