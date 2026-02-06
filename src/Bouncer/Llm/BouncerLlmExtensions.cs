using Bouncer.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bouncer.Llm;

public static class BouncerLlmExtensions
{
    public static IServiceCollection AddBouncerLlm(this IServiceCollection services)
    {
        services.AddSingleton<ILlmJudge>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BouncerOptions>>().Value;
            if (!options.LlmFallback.Enabled)
            {
                return new NullLlmJudge();
            }

            var selection = ProviderDiscovery.SelectProvider(options);
            return selection is null
                ? new NullLlmJudge()
                : new LlmJudge(selection.ChatClient, options.LlmFallback, selection.ProviderOptions);
        });

        return services;
    }
}
