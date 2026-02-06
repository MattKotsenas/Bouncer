namespace Bouncer.Models;

public enum PermissionDecision
{
    Allow,
    Deny
}

public enum EvaluationTier
{
    Rules = 1,
    Llm = 2,
    DefaultAction = 3
}

public sealed record EvaluationResult(PermissionDecision Decision, EvaluationTier Tier, string Reason);
