using System.Reflection;

namespace Bouncer.Commands;

public static class InitCommand
{
    private static readonly string DefaultPath = BouncerPaths.ConfigFile;

    public static async Task<int> ExecuteAsync(
        string? path,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken = default)
    {
        var outputPath = string.IsNullOrWhiteSpace(path) ? DefaultPath : path;
        if (File.Exists(outputPath))
        {
            error.WriteLine($"File already exists: {outputPath}");
            return 1;
        }

        var assembly = typeof(InitCommand).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .Single(name => name.EndsWith(".bouncer.json.example", StringComparison.OrdinalIgnoreCase));
        await using var resourceStream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Embedded example config not found.");

        await using var stream = File.Create(outputPath);
        await resourceStream.CopyToAsync(stream, cancellationToken);
        await stream.FlushAsync(cancellationToken);

        output.WriteLine($"Wrote {outputPath}");
        return 0;
    }
}
