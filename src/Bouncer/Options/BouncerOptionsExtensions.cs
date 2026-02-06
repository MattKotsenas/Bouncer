using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bouncer.Options;

public static class BouncerOptionsExtensions
{
    public static IServiceCollection AddBouncerOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BouncerOptions>()
            .Configure(options => configuration.Bind(options))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<BouncerOptions>, BouncerOptionsValidator>();

        return services;
    }
}
