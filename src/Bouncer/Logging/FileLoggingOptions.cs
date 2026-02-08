namespace Bouncer.Logging;

public sealed class FileLoggingOptions
{
    public const string SectionName = "Logging:File";

    public string Path { get; set; } = ".bouncer/audit.log";
}
