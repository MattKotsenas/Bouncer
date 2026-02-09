using System.Text.Json;
using Bouncer.Models;

namespace Bouncer.Pipeline;

/// <summary>
/// Normalizes platform-specific hook wire formats (Claude Code, Copilot CLI)
/// to/from canonical Bouncer models.
/// </summary>
public interface IHookAdapter
{
    bool CanHandle(JsonElement root);

    HookInput? ReadInput(JsonElement root);

    Task WriteOutputAsync(Stream output, EvaluationResult result, CancellationToken cancellationToken);

    int GetExitCode(EvaluationResult result);
}
