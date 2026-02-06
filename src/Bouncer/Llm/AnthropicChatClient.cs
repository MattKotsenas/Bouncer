using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Bouncer.Models;
using Microsoft.Extensions.AI;

namespace Bouncer.Llm;

internal sealed partial class AnthropicChatClient : IChatClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly bool _disposeClient;

    public AnthropicChatClient(HttpClient httpClient, string model, bool disposeClient)
    {
        _httpClient = httpClient;
        _model = model;
        _disposeClient = disposeClient;
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ChatOptions();
        var request = new AnthropicRequest
        {
            Model = _model,
            MaxTokens = options.MaxOutputTokens ?? 150,
            System = options.Instructions,
            Messages = messages.Select(MapMessage).ToList()
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "v1/messages",
            request,
            AnthropicJsonContext.Default.AnthropicRequest,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync(
            AnthropicJsonContext.Default.AnthropicResponse,
            cancellationToken);

        var text = payload?.Content.FirstOrDefault()?.Text ?? string.Empty;
        var message = new ChatMessage(ChatRole.Assistant, text);
        return new ChatResponse(message);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        AsyncEnumerable.Empty<ChatResponseUpdate>();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
        if (_disposeClient)
        {
            _httpClient.Dispose();
        }
    }

    private static AnthropicMessage MapMessage(ChatMessage message)
    {
        var role = message.Role == ChatRole.Assistant ? "assistant" : "user";
        return new AnthropicMessage(role, message.Text ?? string.Empty);
    }

    private sealed class AnthropicRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; init; }

        [JsonPropertyName("system")]
        public string? System { get; init; }

        [JsonPropertyName("messages")]
        public List<AnthropicMessage> Messages { get; init; } = [];
    }

    private sealed record AnthropicMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public List<AnthropicContent> Content { get; init; } = [];
    }

    private sealed record AnthropicContent(
        [property: JsonPropertyName("text")] string? Text);

    [JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(AnthropicRequest))]
    [JsonSerializable(typeof(AnthropicResponse))]
    private partial class AnthropicJsonContext : JsonSerializerContext
    {
    }
}
