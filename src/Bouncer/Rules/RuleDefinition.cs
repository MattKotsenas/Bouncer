namespace Bouncer.Rules;

public enum ToolField
{
    Command,
    Path,
    Content,
    Pattern,
    Query,
    Url
}

public sealed record RuleDefinition(
    string Name,
    string ToolName,
    ToolField Field,
    string Pattern,
    string Reason);

public sealed record RuleGroupDefinition(
    string Name,
    IReadOnlyList<RuleDefinition> Rules);
