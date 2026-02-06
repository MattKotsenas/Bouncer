using Bouncer.Models;

namespace Bouncer.Rules;

public interface IRuleEngine
{
    RuleMatch? Evaluate(HookInput input);
}

public sealed record RuleMatch(
    string GroupName,
    RuleDefinition Rule,
    PermissionDecision Decision,
    string Reason);
