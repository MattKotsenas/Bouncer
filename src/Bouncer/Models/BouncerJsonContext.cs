using System.Text.Json.Serialization;

namespace Bouncer.Models;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(HookInput))]
[JsonSerializable(typeof(HookOutput))]
[JsonSerializable(typeof(HookSpecificOutput))]
[JsonSerializable(typeof(ToolInput))]
public partial class BouncerJsonContext : JsonSerializerContext
{
}
