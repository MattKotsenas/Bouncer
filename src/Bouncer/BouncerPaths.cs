namespace Bouncer;

public static class BouncerPaths
{
    private static readonly string Home = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".bouncer");

    public static string ConfigFile => Path.Combine(Home, "config.json");

    public static string LogFile() =>
        Path.Combine(Home, "logs", $"{DateTime.UtcNow:yyyy-MM-dd}.log");
}
