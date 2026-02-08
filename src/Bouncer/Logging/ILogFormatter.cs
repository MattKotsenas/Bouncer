using Microsoft.Extensions.Logging;
using System.IO;

namespace Bouncer.Logging;

public interface ILogFormatter
{
    void Write<TState>(in LogEntry<TState> logEntry, Stream stream);
}
