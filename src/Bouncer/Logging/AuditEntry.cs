using Bouncer.Models;

namespace Bouncer.Logging;

public sealed record AuditEntry(
    DateTimeOffset Timestamp,
    string ToolName,
    string ToolInput,
    PermissionDecision Decision,
    EvaluationTier Tier,
    string Reason);
