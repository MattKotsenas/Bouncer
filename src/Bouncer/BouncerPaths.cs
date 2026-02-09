using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Bouncer;

public static partial class BouncerPaths
{
    private static readonly string Home = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".bouncer");

    public static string ConfigFile => Path.Combine(Home, "config.json");

    public static string LogFile() => LogFile(Environment.CurrentDirectory);

    public static string LogFile(string workingDirectory)
    {
        var fullPath = Path.GetFullPath(workingDirectory);
        var name = Path.GetFileName(fullPath) ?? "unknown";
        var hash = ShortHash(fullPath);
        return Path.Combine(Home, "logs", $"{Sanitize(name)}-{hash}", "audit.log");
    }

    private static string ShortHash(string path)
    {
        // Normalize separators and case so the same directory hashes identically on all platforms.
        var normalized = path.Replace('\\', '/').ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes, 0, 4).ToLowerInvariant();
    }

    [GeneratedRegex(@"[^a-zA-Z0-9_\-.]")]
    private static partial Regex UnsafeChars();

    private static string Sanitize(string name) =>
        UnsafeChars().Replace(name, "_");
}
