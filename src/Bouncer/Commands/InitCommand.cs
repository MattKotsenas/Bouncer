using System.Text.Json;
using Bouncer.Options;

namespace Bouncer.Commands;

public static class InitCommand
{
    private const string DefaultPath = ".bouncer.json";
    private const string SchemaUrl = "https://raw.githubusercontent.com/.../bouncer-schema.json";

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

        var options = new BouncerOptions
        {
            Schema = SchemaUrl
        };

        await using var stream = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(
            stream,
            options,
            BouncerOptionsJsonContext.Default.BouncerOptions,
            cancellationToken);
        await stream.FlushAsync(cancellationToken);

        output.WriteLine($"Wrote {outputPath}");
        return 0;
    }
}
