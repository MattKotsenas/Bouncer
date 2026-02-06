using Bouncer.Pipeline;

namespace Bouncer.Commands;

public static class HookCommand
{
    public static Task<int> ExecuteAsync(IBouncerPipeline pipeline, CancellationToken cancellationToken = default) =>
        pipeline.RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput(), cancellationToken);
}
