using Bouncer.Models;

namespace Bouncer.Llm;

public sealed class NullLlmJudge : ILlmJudge
{
    public Task<LlmDecision?> EvaluateAsync(HookInput input, CancellationToken cancellationToken = default) =>
        Task.FromResult<LlmDecision?>(null);
}
