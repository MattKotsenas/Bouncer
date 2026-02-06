using Bouncer.Models;

namespace Bouncer.Pipeline;

public interface IBouncerPipeline
{
    Task<EvaluationResult> EvaluateAsync(HookInput input, CancellationToken cancellationToken = default);

    Task<int> RunAsync(Stream input, Stream output, CancellationToken cancellationToken = default);
}
