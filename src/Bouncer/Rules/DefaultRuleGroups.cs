namespace Bouncer.Rules;

public static class DefaultRuleGroups
{
    private const string SecretPathPattern =
        @"(?i)(^|[\\/])\.env(\.|$)|(^|[\\/])\.env\.production(\.|$)|\.(pem|key)$";

    private const string SecretCommandPattern =
        @"(?i)\b(cat|type|more|less|head|tail)\b.*(\.env(\.|$)|\.env\.production(\.|$)|\.(pem|key))";

    private const string SecretCurlPattern =
        @"(?i)\bcurl\b.*\s(--data|-d)\b.*(@)?(\.env(\.|$)|\.env\.production(\.|$)|\.(pem|key))";

    public static IReadOnlyList<RuleGroupDefinition> All { get; } =
    [
        new RuleGroupDefinition(
            "destructive-shell",
            [
                new RuleDefinition(
                    "rm-rf-root",
                    "bash",
                    ToolField.Command,
                    @"\brm\b\s+(-rf|-fr|--recursive\s+--force|--force\s+--recursive)\s+(/|~|\*)",
                    "Destructive recursive delete"),
                new RuleDefinition(
                    "mkfs",
                    "bash",
                    ToolField.Command,
                    @"\bmkfs(\.\w+)?\b",
                    "Filesystem format"),
                new RuleDefinition(
                    "dd-if",
                    "bash",
                    ToolField.Command,
                    @"\bdd\s+if=",
                    "Disk overwrite"),
                new RuleDefinition(
                    "chmod-777",
                    "bash",
                    ToolField.Command,
                    @"\bchmod\b\s+(-R\s+)?777\b",
                    "Permission blowout"),
                new RuleDefinition(
                    "truncate-system-file",
                    "bash",
                    ToolField.Command,
                    @"(:\s*>\s*/etc/(passwd|shadow))|\btruncate\b.*\s/(etc/passwd|etc/shadow)",
                    "System file truncation")
            ]),
        new RuleGroupDefinition(
            "dangerous-git",
            [
                new RuleDefinition(
                    "git-force-push",
                    "bash",
                    ToolField.Command,
                    @"\bgit\b\s+push\b.*\s(--force\b|--force-with-lease\b)",
                    "Force push to remote"),
                new RuleDefinition(
                    "git-reset-hard-main",
                    "bash",
                    ToolField.Command,
                    @"\bgit\b\s+reset\b.*\s--hard\b.*\b(main|master|release)\b",
                    "Hard reset on protected branch"),
                new RuleDefinition(
                    "git-clean-fdx",
                    "bash",
                    ToolField.Command,
                    @"\bgit\b\s+clean\b.*\s-fdx\b",
                    "Remove untracked files"),
                new RuleDefinition(
                    "git-checkout-discard",
                    "bash",
                    ToolField.Command,
                    @"\bgit\b\s+checkout\b.*\s--\s+\.",
                    "Discard working tree changes")
            ]),
        new RuleGroupDefinition(
            "secrets-exposure",
            [
                new RuleDefinition(
                    "secret-file-write",
                    "write",
                    ToolField.Path,
                    SecretPathPattern,
                    "Secret file modification"),
                new RuleDefinition(
                    "secret-file-edit",
                    "edit",
                    ToolField.Path,
                    SecretPathPattern,
                    "Secret file modification"),
                new RuleDefinition(
                    "secret-file-read",
                    "read",
                    ToolField.Path,
                    SecretPathPattern,
                    "Secret file access"),
                new RuleDefinition(
                    "secret-shell-read",
                    "bash",
                    ToolField.Command,
                    SecretCommandPattern,
                    "Secret file access via shell"),
                new RuleDefinition(
                    "secret-curl-post",
                    "bash",
                    ToolField.Command,
                    SecretCurlPattern,
                    "Secret content exfiltration")
            ]),
        new RuleGroupDefinition(
            "production-risk",
            [
                new RuleDefinition(
                    "curl-delete-prod",
                    "bash",
                    ToolField.Command,
                    @"\bcurl\b.*\s-X\s*DELETE\b.*(prod|production)",
                    "Production DELETE request"),
                new RuleDefinition(
                    "db-drop-truncate",
                    "bash",
                    ToolField.Command,
                    @"\b(drop\s+database|drop\s+table|truncate\s+table)\b",
                    "Destructive database command"),
                new RuleDefinition(
                    "kubectl-delete-prod",
                    "bash",
                    ToolField.Command,
                    @"\bkubectl\b\s+delete\b.*\b(--namespace|-n)\s*(prod|production)\b",
                    "Kubernetes delete in production namespace"),
                new RuleDefinition(
                    "kubectl-apply-no-dryrun",
                    "bash",
                    ToolField.Command,
                    @"\bkubectl\b\s+apply\b(?!.*--dry-run)",
                    "Kubernetes apply without dry-run")
            ])
    ];
}
