namespace Bouncer.Logging;

public sealed class NullAuditLog : IAuditLog
{
    public Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
