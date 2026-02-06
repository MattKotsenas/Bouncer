using Bouncer.Models;

namespace Bouncer.Llm;

public sealed record LlmDecision(
    PermissionDecision Decision,
    string Reason,
    double Confidence);

public interface ILlmJudge
{
    Task<LlmDecision?> EvaluateAsync(HookInput input, CancellationToken cancellationToken = default);
}
