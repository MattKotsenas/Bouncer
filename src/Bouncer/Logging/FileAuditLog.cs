using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bouncer.Logging;

public sealed partial class FileAuditLog : IAuditLog
{
    private readonly string _path;

    public FileAuditLog(string path)
    {
        _path = path;
    }

    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(
            _path,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        await JsonSerializer.SerializeAsync(
            stream,
            entry,
            AuditLogJsonContext.Default.AuditEntry,
            cancellationToken);

        await stream.WriteAsync(Encoding.UTF8.GetBytes(Environment.NewLine), cancellationToken);
    }

    [JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(AuditEntry))]
    private partial class AuditLogJsonContext : JsonSerializerContext
    {
    }
}
