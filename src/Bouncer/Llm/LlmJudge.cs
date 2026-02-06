using System.Text.Json;
using Bouncer.Models;
using Bouncer.Options;
using Microsoft.Extensions.AI;

namespace Bouncer.Llm;

public sealed class LlmJudge : ILlmJudge
{
    private const string SystemPrompt =
        "You are a safety classifier. Decide if a tool invocation should be allowed or denied. " +
        "Respond ONLY with JSON: {\"decision\":\"allow|deny\",\"reason\":\"...\",\"confidence\":0.0-1.0}. " +
        "Decision must be lowercase.";

    private readonly IChatClient _chatClient;
    private readonly LlmFallbackOptions _fallbackOptions;
    private readonly LlmProviderOptions _providerOptions;

    public LlmJudge(IChatClient chatClient, LlmFallbackOptions fallbackOptions, LlmProviderOptions providerOptions)
    {
        _chatClient = chatClient;
        _fallbackOptions = fallbackOptions;
        _providerOptions = providerOptions;
    }

    public async Task<LlmDecision?> EvaluateAsync(HookInput input, CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(input);
        var chatOptions = new ChatOptions
        {
            Instructions = SystemPrompt,
            MaxOutputTokens = _fallbackOptions.MaxTokens,
            Temperature = 0,
            ModelId = _providerOptions.Model
        };

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_providerOptions.TimeoutSeconds));

        ChatResponse response;
        try
        {
            response = await _chatClient.GetResponseAsync(prompt, chatOptions, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        var raw = response.Text?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        LlmDecisionResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize(raw, LlmJsonContext.Default.LlmDecisionResponse);
        }
        catch (JsonException)
        {
            return null;
        }

        if (parsed is null)
        {
            return null;
        }

        if (!TryParseDecision(parsed.Decision, out var decision))
        {
            return null;
        }

        var confidence = parsed.Confidence ?? 0;
        if (confidence < _fallbackOptions.ConfidenceThreshold)
        {
            return null;
        }

        var reason = string.IsNullOrWhiteSpace(parsed.Reason)
            ? "LLM decision"
            : parsed.Reason;

        return new LlmDecision(decision, reason, confidence);
    }

    private static bool TryParseDecision(string? decision, out PermissionDecision value)
    {
        if (string.Equals(decision, "deny", StringComparison.OrdinalIgnoreCase))
        {
            value = PermissionDecision.Deny;
            return true;
        }

        if (string.Equals(decision, "allow", StringComparison.OrdinalIgnoreCase))
        {
            value = PermissionDecision.Allow;
            return true;
        }

        value = default;
        return false;
    }

    private static string BuildPrompt(HookInput input)
    {
        var toolInputJson = JsonSerializer.Serialize(input.ToolInput, BouncerJsonContext.Default.ToolInput);
        return $"tool_name: {input.ToolName}\ntool_input: {toolInputJson}";
    }
}
