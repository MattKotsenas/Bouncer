using Microsoft.Extensions.DependencyInjection;

namespace Bouncer.Pipeline;

public static class BouncerPipelineExtensions
{
    public static IServiceCollection AddBouncerPipeline(this IServiceCollection services)
    {
        services.AddSingleton<IHookAdapter, ClaudeHookAdapter>();
        services.AddSingleton<IHookAdapter, CopilotHookAdapter>();
        services.AddSingleton<IHookAdapterFactory, HookAdapterFactory>();
        services.AddSingleton<IBouncerPipeline, BouncerPipeline>();
        return services;
    }
}
