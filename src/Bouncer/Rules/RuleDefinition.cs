using System.Text.RegularExpressions;

namespace Bouncer.Rules;

public enum ToolField
{
    Command,
    Path,
    Content,
    Pattern,
    Query,
    Url,
    ToolName,
    Unknown
}

public sealed record RuleDefinition(
    string Name,
    string ToolName,
    ToolField Field,
    string Pattern,
    string Action,
    string Reason,
    Func<Regex>? RegexFactory = null);

public sealed record RuleGroupDefinition(
    string Name,
    IReadOnlyList<RuleDefinition> Rules);
