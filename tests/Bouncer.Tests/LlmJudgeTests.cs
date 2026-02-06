using Bouncer.Llm;
using Bouncer.Models;
using Bouncer.Options;
using FluentAssertions;
using Microsoft.Extensions.AI;

namespace Bouncer.Tests;

[TestClass]
public sealed class LlmJudgeTests
{
    [TestMethod]
    public async Task EvaluateAsync_ReturnsDecision_WhenConfident()
    {
        var judge = CreateJudge("{\"decision\":\"deny\",\"reason\":\"unsafe\",\"confidence\":0.9}", 0.7);

        var decision = await judge.EvaluateAsync(HookInput.Bash("rm -rf /"));

        decision.Should().NotBeNull();
        decision!.Decision.Should().Be(PermissionDecision.Deny);
        decision.Reason.Should().Be("unsafe");
    }

    [TestMethod]
    public async Task EvaluateAsync_ReturnsNull_WhenConfidenceTooLow()
    {
        var judge = CreateJudge("{\"decision\":\"allow\",\"reason\":\"ok\",\"confidence\":0.1}", 0.7);

        var decision = await judge.EvaluateAsync(HookInput.Bash("echo ok"));

        decision.Should().BeNull();
    }

    [TestMethod]
    public async Task EvaluateAsync_ReturnsNull_WhenResponseIsMalformed()
    {
        var judge = CreateJudge("not json", 0.7);

        var decision = await judge.EvaluateAsync(HookInput.Bash("echo ok"));

        decision.Should().BeNull();
    }

    private static LlmJudge CreateJudge(string responseText, double threshold)
    {
        var fallback = new LlmFallbackOptions
        {
            MaxTokens = 50,
            ConfidenceThreshold = threshold
        };

        var provider = new LlmProviderOptions
        {
            Model = "gpt-4o-mini",
            TimeoutSeconds = 2
        };

        return new LlmJudge(new FakeChatClient(responseText), fallback, provider);
    }

    private sealed class FakeChatClient : IChatClient
    {
        private readonly string _responseText;

        public FakeChatClient(string responseText)
        {
            _responseText = responseText;
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _responseText)));

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            AsyncEnumerable.Empty<ChatResponseUpdate>();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
