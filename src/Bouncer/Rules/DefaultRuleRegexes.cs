using System.Text.RegularExpressions;

namespace Bouncer.Rules;

public static partial class DefaultRuleRegexes
{
    public const string RmRfRootPattern =
        @"\brm\b\s+(-rf|-fr|--recursive\s+--force|--force\s+--recursive)\s+(/|~|\*)";

    public const string MkfsPattern = @"\bmkfs(\.\w+)?\b";

    public const string DdIfPattern = @"\bdd\s+if=";

    public const string Chmod777Pattern = @"\bchmod\b\s+(-R\s+)?777\b";

    public const string TruncateSystemFilePattern =
        @"(:\s*>\s*/etc/(passwd|shadow))|\btruncate\b.*\s/(etc/passwd|etc/shadow)";

    public const string GitForcePushPattern = @"\bgit\b\s+push\b.*\s(--force\b|--force-with-lease\b)";

    public const string GitResetHardPattern =
        @"\bgit\b\s+reset\b.*\s--hard\b.*\b(main|master|release)\b";

    public const string GitCleanFdxPattern = @"\bgit\b\s+clean\b.*\s-fdx\b";

    public const string GitCheckoutDiscardPattern = @"\bgit\b\s+checkout\b.*\s--\s+\.";

    public const string SecretPathPattern =
        @"(?i)(^|[\\/])\.env(\.|$)|(^|[\\/])\.env\.production(\.|$)|\.(pem|key)$";

    public const string SecretCommandPattern =
        @"(?i)\b(cat|type|more|less|head|tail)\b.*(\.env(\.|$)|\.env\.production(\.|$)|\.(pem|key))";

    public const string SecretCurlPattern =
        @"(?i)\bcurl\b.*\s(--data|-d)\b.*(@)?(\.env(\.|$)|\.env\.production(\.|$)|\.(pem|key))";

    public const string BouncerConfigPathPattern = @"(^|[\\/])\.bouncer\.json$";

    public const string WebFetchPastePattern =
        @"(?i)https?://(pastebin\.com|gist\.github\.com|hastebin\.com|pastie\.org)";

    public const string CurlDeleteProdPattern = @"\bcurl\b.*\s-X\s*DELETE\b.*(prod|production)";

    public const string DbDropTruncatePattern = @"\b(drop\s+database|drop\s+table|truncate\s+table)\b";

    public const string KubectlDeleteProdPattern =
        @"\bkubectl\b\s+delete\b.*\b(--namespace|-n)\s*(prod|production)\b";

    public const string KubectlApplyNoDryRunPattern = @"\bkubectl\b\s+apply\b(?!.*--dry-run)";

    public const string PowerShellRemoveItemRootPattern =
        @"\bRemove-Item\b.*\s(-Recurse|-r)\b.*\s(-Force|-f)\b.*\s(""|')?" +
        @"(?:[A-Za-z]:\\$|[A-Za-z]:\\(Windows|System32|Program\s+Files|Users)\b|/|\\)(?:""|')?";

    public const string PowerShellRemoveItemSystemRootPattern =
        @"\bRemove-Item\b.*\s(-Recurse|-r)\b.*\s(-Force|-f)\b.*\b(\$env:SystemRoot|\$env:WinDir)\b";

    public const string PowerShellDiskWipePattern =
        @"\b(Format-Volume|Clear-Disk|Initialize-Disk|Reset-PhysicalDisk)\b";

    public const string PowerShellExecutionPolicyBypassPattern =
        @"\bSet-ExecutionPolicy\b.*\b(Bypass|Unrestricted)\b";

    public const string PowerShellInvokeExpressionPattern = @"\b(Invoke-Expression|iex)\b";

    public const string PowerShellRegistryDeletePattern =
        @"\breg\b\s+delete\b.*\b(HKLM|HKEY_LOCAL_MACHINE|HKCU|HKEY_CURRENT_USER)\b";

    public const string PowerShellStopRestartPattern = @"\b(Stop-Computer|Restart-Computer)\b";

    public const string SafeBashInfoPattern =
        @"^\s*(ls|pwd|whoami|uname|date|id|which|echo)\b[^;&|<>$`]*$";

    public const string SafeGitReadonlyPattern =
        @"^\s*git\s+(status|diff|log|branch|rev-parse|describe)\b[^;&|<>$`]*$";

    public const string SafeReadPathPattern =
        @"(^|[\\/]).+\.(md|txt|cs|csproj|sln|slnx|props|targets|json|yml|yaml|toml|xml|ini|config|editorconfig|gitignore)$" +
        @"|(^|[\\/])(readme|license)(\.[a-z0-9]+)?$";

    public const string PowerShellSafeInfoPattern =
        @"^\s*(Get-ChildItem|Get-Location|Get-Process|Get-Service|Get-Command|Get-Help|Get-Date|Get-Host|" +
        @"Get-Item|Test-Path|Resolve-Path|Get-PSDrive|Get-PSVersionTable)\b[^;&|<>$`]*$";

    public const string PowerShellSafeReadPattern =
        @"^\s*(Get-Content|Select-String)\b[^;&|<>$`]*(?:" + SafeReadPathPattern + @")[^;&|<>$`]*$";

    public const string SafeNonEmptyPattern = @"^.+$";

    public const string SafeWebFetchPattern = @"^https?://";

    [GeneratedRegex(RmRfRootPattern, RegexOptions.IgnoreCase)]
    public static partial Regex RmRfRoot();

    [GeneratedRegex(MkfsPattern, RegexOptions.IgnoreCase)]
    public static partial Regex Mkfs();

    [GeneratedRegex(DdIfPattern, RegexOptions.IgnoreCase)]
    public static partial Regex DdIf();

    [GeneratedRegex(Chmod777Pattern, RegexOptions.IgnoreCase)]
    public static partial Regex Chmod777();

    [GeneratedRegex(TruncateSystemFilePattern, RegexOptions.IgnoreCase)]
    public static partial Regex TruncateSystemFile();

    [GeneratedRegex(GitForcePushPattern, RegexOptions.IgnoreCase)]
    public static partial Regex GitForcePush();

    [GeneratedRegex(GitResetHardPattern, RegexOptions.IgnoreCase)]
    public static partial Regex GitResetHard();

    [GeneratedRegex(GitCleanFdxPattern, RegexOptions.IgnoreCase)]
    public static partial Regex GitCleanFdx();

    [GeneratedRegex(GitCheckoutDiscardPattern, RegexOptions.IgnoreCase)]
    public static partial Regex GitCheckoutDiscard();

    [GeneratedRegex(SecretPathPattern, RegexOptions.IgnoreCase)]
    public static partial Regex SecretPath();

    [GeneratedRegex(BouncerConfigPathPattern, RegexOptions.IgnoreCase)]
    public static partial Regex BouncerConfigPath();

    [GeneratedRegex(SecretCommandPattern, RegexOptions.IgnoreCase)]
    public static partial Regex SecretCommand();

    [GeneratedRegex(SecretCurlPattern, RegexOptions.IgnoreCase)]
    public static partial Regex SecretCurl();

    [GeneratedRegex(WebFetchPastePattern, RegexOptions.IgnoreCase)]
    public static partial Regex WebFetchPaste();

    [GeneratedRegex(CurlDeleteProdPattern, RegexOptions.IgnoreCase)]
    public static partial Regex CurlDeleteProd();

    [GeneratedRegex(DbDropTruncatePattern, RegexOptions.IgnoreCase)]
    public static partial Regex DbDropTruncate();

    [GeneratedRegex(KubectlDeleteProdPattern, RegexOptions.IgnoreCase)]
    public static partial Regex KubectlDeleteProd();

    [GeneratedRegex(KubectlApplyNoDryRunPattern, RegexOptions.IgnoreCase)]
    public static partial Regex KubectlApplyNoDryRun();

    [GeneratedRegex(PowerShellRemoveItemRootPattern, RegexOptions.IgnoreCase)]
    public static partial Regex PowerShellRemoveItemRoot();

    [GeneratedRegex(PowerShellRemoveItemSystemRootPattern, RegexOptions.IgnoreCase)]
    public static partial Regex PowerShellRemoveItemSystemRoot();

    [GeneratedRegex(PowerShellDiskWipePattern, RegexOptions.IgnoreCase)]
    public static partial Regex PowerShellDiskWipe();

    [GeneratedRegex(PowerShellExecutionPolicyBypassPattern, RegexOptions.IgnoreCase)]
    public static partial Regex PowerShellExecutionPolicyBypass();

    [GeneratedRegex(PowerShellInvokeExpressionPattern, RegexOptions.IgnoreCase)]
    public static partial Regex PowerShellInvokeExpression();

    [GeneratedRegex(PowerShellRegistryDeletePattern, RegexOptions.IgnoreCase)]
    public static partial Regex PowerShellRegistryDelete();

    [GeneratedRegex(PowerShellStopRestartPattern, RegexOptions.IgnoreCase)]
    public static partial Regex PowerShellStopRestart();

    [GeneratedRegex(SafeBashInfoPattern, RegexOptions.IgnoreCase)]
    public static partial Regex SafeBashInfo();

    [GeneratedRegex(SafeGitReadonlyPattern, RegexOptions.IgnoreCase)]
    public static partial Regex SafeGitReadonly();

    [GeneratedRegex(SafeReadPathPattern, RegexOptions.IgnoreCase)]
    public static partial Regex SafeReadPath();

    [GeneratedRegex(PowerShellSafeInfoPattern, RegexOptions.IgnoreCase)]
    public static partial Regex PowerShellSafeInfo();

    [GeneratedRegex(PowerShellSafeReadPattern, RegexOptions.IgnoreCase)]
    public static partial Regex PowerShellSafeRead();

    [GeneratedRegex(SafeNonEmptyPattern, RegexOptions.IgnoreCase)]
    public static partial Regex SafeNonEmpty();

    [GeneratedRegex(SafeWebFetchPattern, RegexOptions.IgnoreCase)]
    public static partial Regex SafeWebFetch();
}
